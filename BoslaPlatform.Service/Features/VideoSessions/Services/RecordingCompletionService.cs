using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.VideoSessions.Services;

/// <summary>
/// The one and only recording-completion pipeline. See <see cref="IRecordingCompletionService"/>.
/// </summary>
public sealed class RecordingCompletionService : IRecordingCompletionService
{
    private readonly IAppDbContext _context;
    private readonly IRecordingProvider _provider;
    private readonly IRecordingUploadVerifier _verifier;
    private readonly IRecordingStorageSettings _storageSettings;
    private readonly IRecordingPipelineLog _pipelineLog;
    private readonly ILogger<RecordingCompletionService> _logger;

    public RecordingCompletionService(
        IAppDbContext context,
        IRecordingProvider provider,
        IRecordingUploadVerifier verifier,
        IRecordingStorageSettings storageSettings,
        IRecordingPipelineLog pipelineLog,
        ILogger<RecordingCompletionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _storageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
        _pipelineLog = pipelineLog ?? throw new ArgumentNullException(nameof(pipelineLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RecordingCompletionOutcome>> CompleteAsync(
        Guid videoSessionId,
        RecordingCompletionTrigger trigger,
        RecordingProviderHint? hint = null,
        CancellationToken ct = default)
    {
        // ── Step 1: read identifiers + short-circuit idempotently ──────────────
        // Identifiers come from the SESSION aggregate (the source of truth for the
        // recording lifecycle), not the ScreenRecording row — a webhook-driven
        // recording may have no ScreenRecording entity yet.
        var snapshot = await _context.VideoSessions
            .AsNoTracking()
            .Where(s => s.Id == videoSessionId)
            .Select(s => new
            {
                s.Id,
                s.AppointmentId,
                s.AgoraChannelName,
                s.RecordingStatus,
                s.CurrentRecordingId,
                s.AgoraRecordingId,
                s.AgoraRecordingSid,
                s.AgoraRecordingUid
            })
            .FirstOrDefaultAsync(ct);

        if (snapshot is null)
            return Error.NotFound("VideoSession.NotFound", "Video session was not found.");

        if (snapshot.RecordingStatus is null)
        {
            // Nothing was ever recorded — there is nothing to complete.
            return new RecordingCompletionOutcome(
                RecordingStatus.Idle, UploadVerificationOutcome.Pending);
        }

        // Idempotency: a recording already finalized (verified Completed) must never
        // be reprocessed. This is what makes duplicate webhooks and completion races
        // (manual Stop vs expiration vs webhook) safe to all call this one method.
        if (snapshot.RecordingStatus is RecordingStatus.Completed)
        {
            return new RecordingCompletionOutcome(
                RecordingStatus.Completed, UploadVerificationOutcome.Verified);
        }

        var channel = snapshot.AgoraChannelName;
        var resourceId = snapshot.AgoraRecordingId ?? string.Empty;
        var sid = snapshot.AgoraRecordingSid ?? string.Empty;
        var uid = snapshot.AgoraRecordingUid ?? string.Empty;
        var bucket = _storageSettings.RecordingBucketName;

        var objectKey = hint?.ObjectKey; //null
        var uploadingStatus = hint?.UploadingStatus ?? AgoraUploadingStatus.Unknown; //unknown
        var durationSeconds = hint?.DurationSeconds ?? 0; //0

        var logContext = new RecordingLogContext
        {
            SessionId = snapshot.Id,
            AppointmentId = snapshot.AppointmentId,
            RecordingId = snapshot.CurrentRecordingId,
            RecordingCorrelationId = snapshot.CurrentRecordingId is { } rid
                ? RecordingLogContext.ForRecording(rid)
                : null,
            ChannelName = channel,
            ResourceId = resourceId,
            Sid = sid,
            RecordingUid = uid
        };

        // ── Step 2: (optionally) issue provider Stop ──────────────────────────
        // Stop-initiated triggers must end the still-running recording. The webhook
        // trigger must NOT stop (the recording already ended — a Stop would 404 and
        // return an empty file list, masking a genuinely uploaded recording).
        var issueStop = trigger is RecordingCompletionTrigger.ManualStop
            or RecordingCompletionTrigger.SessionEnded; //true

        if (issueStop)
        {
            if (string.IsNullOrWhiteSpace(resourceId) ||
                string.IsNullOrWhiteSpace(sid) ||
                string.IsNullOrWhiteSpace(uid))
            {
                return Error.Validation(
                    "Recording.MissingProviderIdentifiers",
                    "Cannot stop recording: provider ResourceId/SID/UID is missing.");
            }

            var stop = await _provider.StopRecordingAsync(channel, resourceId, sid, uid, ct);
            if (stop.IsError)
            {
                // Do not mutate DB — leave the recording as-is for a later retry.
                _logger.LogWarning(
                    "Completion({Trigger}): provider stop failed for session {SessionId}: {Error}",
                    trigger, videoSessionId, stop.Errors[0].Description);
                return stop.Errors;
            }

            objectKey ??= stop.Value.Files?.FirstOrDefault()?.ObjectKey; // null
            if (string.IsNullOrWhiteSpace(objectKey)) //true
                objectKey = stop.Value.FileUrl;
            uploadingStatus = stop.Value.UploadingStatus != AgoraUploadingStatus.Unknown
                ? stop.Value.UploadingStatus
                : uploadingStatus; //unknown
            if (stop.Value.DurationSeconds > 0)
                durationSeconds = stop.Value.DurationSeconds;
        }

        // ── Step 3: resolve the object key via Query if still unknown ─────────
        if (string.IsNullOrWhiteSpace(objectKey) &&
            !string.IsNullOrWhiteSpace(resourceId) &&
            !string.IsNullOrWhiteSpace(sid))
        {
            var query = await _provider.QueryAsync(resourceId, sid, ct);
            if (!query.IsError)
            {
                objectKey = query.Value.Files?.FirstOrDefault()?.ObjectKey ?? objectKey;
                if (uploadingStatus == AgoraUploadingStatus.Unknown)
                    uploadingStatus = query.Value.UploadingStatus;
            }
        }

        // ── Step 4: verify the upload (shared verifier — the ONE verification) ─
        // Webhooks do a single immediate check (they are already the async signal);
        // stop-initiated completions use the configured polling budget.
        var attempts = trigger == RecordingCompletionTrigger.WebhookConfirmed ? 1 : (int?)null; //null

        var verification = await _verifier.VerifyAsync(
            resourceId, sid, bucket, objectKey ?? string.Empty, uploadingStatus, attempts, ct);
        //reason : "Agora returned no file — nothing was captured to verify."

        // ── Step 5: persist the final state (the ONE metadata-persistence) ────
        return await PersistAsync(videoSessionId, snapshot.CurrentRecordingId, bucket, durationSeconds, verification, logContext, ct);
    }

    private async Task<Result<RecordingCompletionOutcome>> PersistAsync(
        Guid videoSessionId,
        Guid? recordingId,
        string bucket,
        int durationSeconds,
        UploadVerificationResult verification,
        RecordingLogContext logContext,
        CancellationToken ct)
    {
        var session = await _context.VideoSessions
            .Include(x => x.CurrentRecording)
            .Include(x => x.Recordings)
            .FirstOrDefaultAsync(x => x.Id == videoSessionId, ct);

        if (session is null)
            return Error.NotFound("VideoSession.NotFound", "Video session was not found after stop.");

        // Re-check idempotency on the fresh, tracked entity: a concurrent path may
        // have finalized it between the snapshot read and here.
        if (session.RecordingStatus is RecordingStatus.Completed)
        {
            return new RecordingCompletionOutcome(
                RecordingStatus.Completed, UploadVerificationOutcome.Verified);
        }

        var recording = (recordingId is { } rid
                ? session.Recordings.FirstOrDefault(r => r.Id == rid)
                : null)
            ?? session.CurrentRecording
            ?? session.Recordings.OrderByDescending(r => r.CreatedAtUtc).FirstOrDefault();

        switch (verification.Outcome)
        {
            case UploadVerificationOutcome.Verified:
                var meta = verification.Metadata!;
                var verifiedKey = verification.ObjectKey ?? string.Empty;
                var contentType = meta.ContentType ?? InferContentType(verifiedKey);

                recording?.Complete(verifiedKey, meta.ContentLength, durationSeconds);

                // Status-independent finalize: works whether the recording was still
                // Recording (manual/session-ended) or parked PendingUpload (a later
                // path confirming the upload). Sets Completed + RecordingUrl.
                session.CompleteRecordingVerified(
                    verifiedKey,
                    meta.ContentLength,
                    durationSeconds > 0 ? durationSeconds : null);

                session.SetS3RecordingMetadata(
                    StorageProvider.AmazonS3,
                    bucket,
                    verifiedKey,
                    contentType: contentType,
                    contentLength: meta.ContentLength,
                    durationSeconds: durationSeconds > 0 ? durationSeconds : null,
                    etag: meta.ETag);

                _pipelineLog.Succeeded(
                    RecordingStage.MetadataSaved, logContext,
                    extra: new Dictionary<string, object?>
                    {
                        ["bucket"] = bucket,
                        ["objectKey"] = verifiedKey,
                        ["contentLength"] = meta.ContentLength,
                        ["etag"] = meta.ETag,
                        ["verified"] = true
                    });
                break;

            case UploadVerificationOutcome.Pending:
                session.MarkRecordingUploadPending();
                _pipelineLog.Failed(
                    RecordingStage.MetadataSaved, logContext,
                    "Recording.UploadPending",
                    verification.Reason ?? "Upload not yet confirmed in S3.");
                break;

            case UploadVerificationOutcome.UploadFailed:
                session.MarkRecordingUploadFailed(verification.Reason);
                _pipelineLog.Failed(
                    RecordingStage.MetadataSaved, logContext,
                    "Recording.UploadFailed",
                    verification.Reason ?? "Agora returned no file — nothing was captured.");
                break;

            default: // VerificationFailed
                session.MarkRecordingVerificationFailed(verification.Reason);
                _pipelineLog.Failed(
                    RecordingStage.MetadataSaved, logContext,
                    "Recording.VerificationFailed",
                    verification.Reason ?? "S3 object missing or zero-length.");
                break;
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent completion won. Re-read; if it reached a terminal state
            // that is the identical final state, treat this call as an idempotent success.
            var current = await _context.VideoSessions
                .AsNoTracking()
                .Where(x => x.Id == videoSessionId)
                .Select(x => x.RecordingStatus)
                .FirstOrDefaultAsync(ct);

            if (current is RecordingStatus.Completed)
            {
                return new RecordingCompletionOutcome(
                    RecordingStatus.Completed, UploadVerificationOutcome.Verified);
            }

            throw;
        }

        return new RecordingCompletionOutcome(
            session.RecordingStatus ?? RecordingStatus.PendingUpload,
            verification.Outcome,
            verification.ObjectKey);
    }

    private static string InferContentType(string key) =>
        key.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? "video/mp4"
        : key.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ? "application/vnd.apple.mpegurl"
        : "application/octet-stream";
}
