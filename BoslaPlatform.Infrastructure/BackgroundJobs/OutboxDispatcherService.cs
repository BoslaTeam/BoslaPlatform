using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// A <see cref="BackgroundService"/> that continuously polls the OutboxMessages table
/// for unprocessed entries, deserialises each event, and publishes it through the
/// configured <see cref="IOutboxMessagePublisher"/>.
///
/// <b>Dispatch flow:</b>
///   Loop ─→ Read pending batch (WHERE ProcessedOnUtc IS NULL, ORDER BY OccurredOnUtc)
///         │
///         ├─ Empty → Log → Delay(PollInterval) → continue
///         │
///         └─ Batch → For each message (sequential):
///                     ├─ Resolve CLR type via cached IEventTypeResolver
///                     ├─ Deserialise payload
///                     │    └─ Fail → set LastAttemptUtc + LastError → SaveChanges
///                     ├─ Publish via IOutboxMessagePublisher
///                     │    ├─ Success → ProcessedOnUtc = UtcNow, LastAttemptUtc = UtcNow
///                     │    └─ Throw   → LastAttemptUtc = UtcNow, LastError = ex.Message
///                     └─ SaveChangesAsync
///         │
///         └─ If batch was non-empty → immediately loop (no delay)
///
/// <b>Why sequential processing?</b>
/// Messages within a batch are processed one at a time to avoid:
///   - Overloading the database with concurrent updates
///   - Thundering-herd effects on downstream systems
///   - Complex error recovery with partial batch failures
/// Parallelism can be introduced later if throughput requirements demand it.
///
/// <b>Why no retries?</b>
/// Retries are intentionally absent from this sprint to maintain a clean separation
/// of concerns. The dispatcher records failures via LastError/LastAttemptUtc but does
/// not re-queue or increment RetryCount. A dedicated retry policy (with back-off,
/// circuit breaker, and optional Dead Letter Queue) will be added in a future sprint
/// once the outbox is connected to a real message broker. Premature retry logic would
/// complicate failure diagnostics and risk uncontrolled replay under transient faults.
/// For now, operators monitor LastError and can manually re-queue via SQL if needed.
///
/// <b>Why NoOp publisher?</b>
/// The current publisher implementation (<see cref="NoOpOutboxMessagePublisher"/>)
/// deserialises and logs only. This allows the full dispatch pipeline to be
/// exercised end-to-end without depending on any external infrastructure.
/// </summary>
public sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly OutboxDispatcherOptions _options;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxDispatcherOptions> options,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxDispatcherService started (batchSize: {BatchSize}, pollingInterval: {Interval}s)",
            _options.BatchSize, _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hadMessages = await ProcessBatchAsync(stoppingToken);

                // Only delay when the queue is empty.
                // If we processed a batch, immediately poll for more.
                if (!hadMessages)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                        stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Outbox dispatch cycle failed");
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation("OutboxDispatcherService stopped");
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxMessagePublisher>();
        var typeResolver = scope.ServiceProvider.GetRequiredService<IEventTypeResolver>();

        var batch = await context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            _logger.LogWarning("No pending outbox messages found");
            return false;
        }

        var sw = Stopwatch.StartNew();
        var utcNow = DateTime.UtcNow;
        var processedCount = 0;
        var failedCount = 0;

        foreach (var message in batch)
        {
            ct.ThrowIfCancellationRequested();

            // ── Deserialise ──────────────────────────────────────────
            object? deserializedEvent;
            try
            {
                var eventType = typeResolver.Resolve(message.AssemblyName, message.EventType);
                deserializedEvent = JsonSerializer.Deserialize(
                    message.Payload, eventType, OutboxConstants.SerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Deserialization failed for outbox message {MessageId} ({EventType})",
                    message.Id, message.EventType);

                await RecordFailureAsync(context, message.Id, utcNow, ex.Message);
                failedCount++;
                continue;
            }

            if (deserializedEvent is null)
            {
                _logger.LogError(
                    "Deserialization returned null for outbox message {MessageId} ({EventType})",
                    message.Id, message.EventType);

                await RecordFailureAsync(context, message.Id, utcNow,
                    "Deserialization returned null");
                failedCount++;
                continue;
            }

            // ── Publish ──────────────────────────────────────────────
            try
            {
                var tracked = await context.OutboxMessages
                    .FirstAsync(m => m.Id == message.Id, ct);

                await publisher.PublishAsync(tracked, deserializedEvent, ct);

                tracked.ProcessedOnUtc = utcNow;
                tracked.LastAttemptUtc = utcNow;
                tracked.LastError = null;

                await context.SaveChangesAsync(ct);

                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Publishing failed for outbox message {MessageId} ({EventType})",
                    message.Id, message.EventType);

                await RecordFailureAsync(context, message.Id, utcNow, ex.Message);
                failedCount++;
            }
        }

        sw.Stop();

        _logger.LogInformation(
            "Processed batch: BatchSize={BatchSize}, Processed={ProcessedCount}, Failed={FailedCount}, Elapsed={ElapsedMs}ms",
            batch.Count, processedCount, failedCount, sw.ElapsedMilliseconds);

        return true;
    }

    private static async Task RecordFailureAsync(
        AppDbContext context, Guid messageId, DateTime utcNow, string error)
    {
        try
        {
            var tracked = await context.OutboxMessages
                .FirstAsync(m => m.Id == messageId);

            tracked.LastAttemptUtc = utcNow;
            tracked.LastError = error;

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Failure recording is best-effort. The outbox entry already has
            // its previous state preserved. Log and continue processing.
        }
    }
}
