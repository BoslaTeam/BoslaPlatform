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
///   Loop ─→ Read pending batch
///         │  WHERE ProcessedOnUtc IS NULL
///         │  AND (NextRetryUtc IS NULL OR NextRetryUtc &lt;= UtcNow)
///         │  ORDER BY OccurredOnUtc
///         │
///         ├─ Empty → Log → Delay(PollInterval) → continue
///         │
///         └─ Batch → For each message (sequential):
///                     ├─ Resolve CLR type via cached IEventTypeResolver
///                     ├─ Deserialise payload
///                     │    └─ Fail → RecordFailure (with retry/back-off)
///                     ├─ Publish via IOutboxMessagePublisher
///                     │    ├─ Success → ProcessedOnUtc = UtcNow, LastAttemptUtc = UtcNow
///                     │    │            NextRetryUtc = null, LastError = null
///                     │    └─ Throw   → RecordFailure (with retry/back-off)
///                     └─ SaveChangesAsync
///         │
///         └─ If batch was non-empty → immediately loop (no delay)
///
/// <b>Why exponential back-off?</b>
/// Transient failures (network blips, service restarts, throttling) are often
/// resolved within seconds. A small initial delay avoids hammering the downstream
/// system. Exponential growth ensures that persistent failures do not cause
/// infinite busy-retry loops, while the cap guarantees the message is revisited
/// at least every <see cref="OutboxRetryOptions.MaxDelayMinutes"/>.
/// The arithmetic is delegated to <see cref="OutboxRetryCalculator"/>.
///
/// <b>Why failed messages remain in the Outbox table?</b>
/// Failed messages stay in the OutboxMessages table indefinitely so operators
/// can inspect LastError, monitor retry progress, and manually re-queue or
/// remove entries. A future Dead Letter Queue sprint will move permanently
/// failed messages to a separate DLQ table or export them for offline analysis.
///
/// <b>Why Dead Letter Queue is postponed?</b>
/// DLQ requires a separate table, a background migration job, and operational
/// tooling (re-queue from DLQ, TTL-based cleanup, alerting). Adding these
/// before the retry mechanism is stable would risk losing visibility into
/// failure patterns. DLQ will be implemented in Sprint 5.
///
/// <b>Why sequential processing?</b>
/// Messages within a batch are processed one at a time to avoid:
///   - Overloading the database with concurrent updates
///   - Thundering-herd effects on downstream systems
///   - Complex error recovery with partial batch failures
/// Parallelism can be introduced later if throughput requirements demand it.
/// </summary>
public sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly OutboxDispatcherOptions _dispatcherOptions;
    private readonly OutboxRetryOptions _retryOptions;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxDispatcherOptions> dispatcherOptions,
        IOptions<OutboxRetryOptions> retryOptions,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _dispatcherOptions = dispatcherOptions.Value;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxDispatcherService started (batchSize: {BatchSize}, pollingInterval: {Interval}s, " +
            "maxRetries: {MaxRetryCount}, baseDelay: {BaseDelay}s, maxDelay: {MaxDelay}min)",
            _dispatcherOptions.BatchSize, _dispatcherOptions.PollIntervalSeconds,
            _retryOptions.MaxRetryCount, _retryOptions.BaseDelaySeconds, _retryOptions.MaxDelayMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hadMessages = await ProcessBatchAsync(stoppingToken);

                if (!hadMessages)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_dispatcherOptions.PollIntervalSeconds),
                        stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Outbox dispatch cycle failed");
                await Task.Delay(
                    TimeSpan.FromSeconds(_dispatcherOptions.PollIntervalSeconds),
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

        var utcNow = DateTime.UtcNow;

        var batch = await context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedOnUtc == null)
            .Where(m => m.NextRetryUtc == null || m.NextRetryUtc <= utcNow)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_dispatcherOptions.BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            _logger.LogWarning("No pending outbox messages found");
            return false;
        }

        var sw = Stopwatch.StartNew();
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
                tracked.NextRetryUtc = null;
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

    private async Task RecordFailureAsync(
        AppDbContext context, Guid messageId, DateTime utcNow, string error)
    {
        try
        {
            var tracked = await context.OutboxMessages
                .FirstAsync(m => m.Id == messageId);

            tracked.RetryCount++;
            tracked.LastAttemptUtc = utcNow;
            tracked.LastError = error;

            if (tracked.RetryCount >= _retryOptions.MaxRetryCount)
            {
                // Permanently failed — no more retries.
                // NextRetryUtc stays null so the dispatcher skips this message.
                tracked.NextRetryUtc = null;

                _logger.LogWarning(
                    "Retry limit reached for outbox message {MessageId} ({EventType}). " +
                    "RetryCount={RetryCount}, MaxRetryCount={MaxRetryCount}. " +
                    "Message will remain unprocessed until a Dead Letter Queue migration.",
                    tracked.Id, tracked.EventType, tracked.RetryCount, _retryOptions.MaxRetryCount);
            }
            else
            {
                var delaySeconds = OutboxRetryCalculator.CalculateDelay(
                    tracked.RetryCount, _retryOptions);

                tracked.NextRetryUtc = utcNow.AddSeconds(delaySeconds);

                _logger.LogInformation(
                    "Retry scheduled for outbox message {MessageId} ({EventType}). " +
                    "RetryCount={RetryCount}, Delay={DelaySeconds}s, NextRetryUtc={NextRetryUtc}",
                    tracked.Id, tracked.EventType, tracked.RetryCount, delaySeconds, tracked.NextRetryUtc);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to record failure state for outbox message {MessageId}",
                messageId);
        }
    }
}
