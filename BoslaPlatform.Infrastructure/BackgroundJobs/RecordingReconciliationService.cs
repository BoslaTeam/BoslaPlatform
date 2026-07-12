using BoslaPlatform.Application.Features.RecordingTransfer.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically scans for recordings stuck in
/// <see cref="UploadStatus.Pending"/> or <see cref="UploadStatus.Retrying"/> states
/// and re-triggers their upload pipeline.
///
/// Responsibilities:
///   1. Query eligible sessions (Pending/Retrying + NextRetryAtUtc ≤ UtcNow + RetryCount &lt; Max).
///   2. Acquire per-session lock to prevent concurrent reconciliation.
///   3. Query Agora for file availability.
///   4. Trigger RecordingTransferService if files are ready.
///   5. Apply exponential-backoff retry or permanently cancel on exhaustion.
/// </summary>
public class RecordingReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RecordingReconciliationOptions _options;
    private readonly IRecordingLock _lock;
    private readonly ILogger<RecordingReconciliationService> _logger;

    public RecordingReconciliationService(
        IServiceScopeFactory scopeFactory,
        IOptions<RecordingReconciliationOptions> options,
        IRecordingLock @lock,
        ILogger<RecordingReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _lock = @lock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RecordingReconciliationService started. Poll={PollingIntervalSeconds}s, MaxRetry={MaxRetry}, BaseBackoff={BaseBackoff}s, Batch={Batch}",
            _options.PollingIntervalSeconds, _options.MaxRetryAttempts,
            _options.BaseBackoffSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileInternalAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecordingReconciliationService iteration failed unexpectedly");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("RecordingReconciliationService stopped.");
    }

    protected virtual async Task ReconcileInternalAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<IRecordingProvider>();
        var transferService = scope.ServiceProvider.GetRequiredService<RecordingTransferService>();
        var metrics = scope.ServiceProvider.GetRequiredService<IRecordingMetrics>();

        var now = DateTime.UtcNow;

        // Fetch batch of sessions eligible for reconciliation.
        var eligible = await context.VideoSessions
            .Where(s =>
                (s.UploadStatus == UploadStatus.Pending || s.UploadStatus == UploadStatus.Retrying) &&
                (s.NextRetryAtUtc == null || s.NextRetryAtUtc <= now) &&
                s.RetryCount < _options.MaxRetryAttempts &&
                s.AgoraRecordingId != null &&
                s.AgoraRecordingSid != null)
            .OrderBy(s => s.NextRetryAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (eligible.Count == 0)
        {
            _logger.LogDebug("Reconciliation pass: no eligible recordings found");
            return;
        }

        _logger.LogInformation(
            "Reconciliation pass: found {Count} eligible recording(s)", eligible.Count);

        metrics.RecordPendingUploads(eligible.Count);

        foreach (var session in eligible)
        {
            if (ct.IsCancellationRequested) break;

            // Acquire process-level lock (prevents two tasks from processing the same session).
            var lockHandle = await _lock.TryAcquireAsync(session.Id, ct);
            if (lockHandle is null)
            {
                _logger.LogDebug(
                    "Reconciliation skipping session {SessionId} — already locked by another task",
                    session.Id);
                continue;
            }

            await using (lockHandle)
            {
                await ProcessSessionAsync(session.Id,
                    session.AgoraRecordingId!,
                    session.AgoraRecordingSid!,
                    context, provider, transferService, ct);
            }
        }
    }

    private async Task ProcessSessionAsync(
        Guid sessionId,
        string resourceId,
        string sid,
        IAppDbContext context,
        IRecordingProvider provider,
        RecordingTransferService transferService,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Reconciliation processing session {SessionId}, ResourceId={ResourceId}, SID={SID}",
            sessionId, resourceId, sid);

        try
        {
            // Reload session inside this scope to get fresh state.
            var session = await context.VideoSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session is null)
            {
                _logger.LogWarning("Reconciliation: session {SessionId} disappeared before processing", sessionId);
                return;
            }

            // Guard: check RetryCount again (another process may have updated it).
            if (session.RetryCount >= _options.MaxRetryAttempts)
            {
                _logger.LogWarning(
                    "Reconciliation: session {SessionId} has exceeded MaxRetryAttempts ({Max}). Cancelling.",
                    sessionId, _options.MaxRetryAttempts);

                session.MarkUploadCancelled();
                await context.SaveChangesAsync(ct);
                return;
            }

            // Query Agora for recording file status.
            var queryResult = await provider.QueryAsync(resourceId, sid, ct);

            if (queryResult.IsError)
            {
                _logger.LogWarning(
                    "Reconciliation: Agora query failed for session {SessionId}: {Errors}",
                    sessionId, string.Join("; ", queryResult.Errors.Select(e => e.Description)));

                ScheduleRetry(session, RecordingFailureCategory.Transient, context, ct);
                await context.SaveChangesAsync(ct);
                return;
            }

            var files = queryResult.Value.Files;
            var status = queryResult.Value.Status;

            _logger.LogInformation(
                "Reconciliation: Agora status={Status}, FileCount={FileCount} for session {SessionId}",
                status, files?.Count ?? 0, sessionId);

            // If Agora reports the recording isn't ready yet, reschedule.
            if (status is RecordingStatus.Processing or RecordingStatus.Uploading or
                RecordingStatus.Idle or RecordingStatus.Starting)
            {
                _logger.LogInformation(
                    "Reconciliation: recording not yet ready for session {SessionId} (status={Status}). Rescheduling.",
                    sessionId, status);

                ScheduleRetry(session, RecordingFailureCategory.Transient, context, ct);
                await context.SaveChangesAsync(ct);
                return;
            }

            if (files is null || files.Count == 0)
            {
                _logger.LogWarning(
                    "Reconciliation: Agora returned no files for session {SessionId} (status={Status}). Rescheduling.",
                    sessionId, status);

                ScheduleRetry(session, RecordingFailureCategory.Transient, context, ct);
                await context.SaveChangesAsync(ct);
                return;
            }

            // Files are available — hand off to the transfer pipeline.
            // RecordingTransferService handles its own idempotency and integrity checks.
            _logger.LogInformation(
                "Reconciliation: triggering transfer for session {SessionId} ({FileCount} file(s))",
                sessionId, files.Count);

            await transferService.TransferRecordingAsync(sessionId, resourceId, sid, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another process won the optimistic concurrency race.
            // Safe to skip — the other process will complete the transfer.
            _logger.LogWarning(
                "Reconciliation: concurrency conflict for session {SessionId} — skipping (another process won)",
                sessionId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Reconciliation: unexpected error processing session {SessionId}", sessionId);

            // Try to schedule a retry for transient errors.
            try
            {
                var session = await context.VideoSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
                if (session is not null)
                {
                    var category = RecordingFailureClassifier.Classify(ex);
                    ScheduleRetry(session, category, context, ct);
                    await context.SaveChangesAsync(ct);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Reconciliation: failed to persist retry state for session {SessionId}", sessionId);
            }
        }
    }

    private void ScheduleRetry(
        Domain.Models.Video.VideoSession session,
        RecordingFailureCategory category,
        IAppDbContext context,
        CancellationToken ct)
    {
        var nextRetry = DateTime.UtcNow.AddSeconds(
            _options.BaseBackoffSeconds * Math.Pow(2, session.RetryCount));

        session.MarkRetryScheduled(nextRetry, category);

        _logger.LogInformation(
            "Reconciliation: session {SessionId} scheduled for retry #{RetryCount} at {NextRetry} (category={Category})",
            session.Id, session.RetryCount, nextRetry, category);
    }
}
