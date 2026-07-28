using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// Drains <see cref="RecordingTelemetryQueue"/> into the diagnostics table in
/// small batches, on its own connection, off the request path. Failures here are
/// logged and swallowed — losing telemetry must never surface to a user.
/// </summary>
internal sealed class RecordingTelemetryWriter : BackgroundService
{
    private readonly RecordingTelemetryQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecordingTelemetryWriter> _logger;

    public RecordingTelemetryWriter(
        RecordingTelemetryQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<RecordingTelemetryWriter> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureTableAsync(stoppingToken);

        try
        {
            await foreach (var evt in _queue.ReadAllAsync(stoppingToken))
            {
                await FlushAsync(evt, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
    }

    private async Task FlushAsync(RecordingPipelineEvent evt, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RecordingDiagnosticsDbContext>();

            db.RecordingPipelineEvents.Add(evt);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RecordingTelemetry] Failed to persist pipeline event {Stage}/{Outcome} for {CorrelationId}; it is lost.",
                evt.Stage, evt.Outcome, evt.RecordingCorrelationId);
        }
    }

    private async Task EnsureTableAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RecordingDiagnosticsDbContext>();
            await db.EnsureTableExistsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RecordingTelemetry] Could not ensure the RecordingPipelineEvents table exists. " +
                "Timeline persistence will fail until this is resolved.");
        }
    }
}
