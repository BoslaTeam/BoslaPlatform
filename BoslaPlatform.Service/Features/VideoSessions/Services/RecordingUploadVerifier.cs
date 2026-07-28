using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Application.Features.VideoSessions.Services;

/// <summary>
/// Default <see cref="IRecordingUploadVerifier"/>: Stop → Query(poll) → HeadObject.
///
/// Contract with the docs:
///   - Agora's Stop/Query <c>uploadingStatus</c> tells us WHEN to look in S3
///     ("uploaded" = all files in our bucket; "backuped"/"backuping" = still in
///     Agora backup, keep waiting).
///   - S3 <c>HeadObject</c> is the ONLY proof that a playable object exists, so a
///     recording is Verified only when HeadObject succeeds with ContentLength &gt; 0.
/// </summary>
public sealed class RecordingUploadVerifier : IRecordingUploadVerifier
{
    private readonly IRecordingProvider _provider;
    private readonly IRecordingStorage _storage;
    private readonly RecordingUploadVerificationOptions _options;
    private readonly ILogger<RecordingUploadVerifier> _logger;

    public RecordingUploadVerifier(
        IRecordingProvider provider,
        IRecordingStorage storage,
        IOptions<RecordingUploadVerificationOptions> options,
        ILogger<RecordingUploadVerifier> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadVerificationResult> VerifyAsync(
        string resourceId,
        string sid,
        string bucketName,
        string objectKey,
        AgoraUploadingStatus initialUploadingStatus,
        int? maxAttempts = null,
        CancellationToken ct = default)
    {
        // No object key means Agora captured/uploaded nothing — there is nothing in
        // S3 to verify and never will be. This is an upload failure, not "pending".
        if (string.IsNullOrWhiteSpace(objectKey) || string.IsNullOrWhiteSpace(bucketName))
        {
            _logger.LogError(
                "Upload verification: no object key/bucket for resourceId={ResourceId}, sid={Sid} — nothing was captured.",
                resourceId, sid);

            return new UploadVerificationResult(
                UploadVerificationOutcome.UploadFailed,
                Reason: "Agora returned no file — nothing was captured to verify.");
        }

        var uploadingStatus = initialUploadingStatus;
        var attemptBudget = Math.Max(1, maxAttempts ?? _options.MaxAttempts);

        for (var attempt = 1; attempt <= attemptBudget; attempt++)
        {
            // Check S3 as soon as Agora claims "uploaded", and also on any later
            // attempt (belt-and-braces: the object may appear before Query flips).
            if (uploadingStatus == AgoraUploadingStatus.Uploaded || attempt > 1)
            {
                var head = await _storage.HeadObjectAsync(bucketName, objectKey, ct);

                if (!head.IsError)
                {
                    if (head.Value.ContentLength > 0)
                    {
                        _logger.LogInformation(
                            "Upload verified for {Bucket}/{ObjectKey}: contentLength={Length}, etag={ETag} (attempt {Attempt}).",
                            bucketName, objectKey, head.Value.ContentLength, head.Value.ETag, attempt);

                        return new UploadVerificationResult(
                            UploadVerificationOutcome.Verified, objectKey, head.Value);
                    }

                    // The object exists but is empty — a corrupt/failed upload, not
                    // something more waiting will fix.
                    _logger.LogError(
                        "Upload verification failed for {Bucket}/{ObjectKey}: object exists but ContentLength is 0.",
                        bucketName, objectKey);

                    return new UploadVerificationResult(
                        UploadVerificationOutcome.VerificationFailed, objectKey, head.Value,
                        "S3 object exists but is zero-length.");
                }

                // A persistent, non-404 S3 error (HeadObject already retried transient
                // failures internally) is a verification failure on the final attempt.
                if (head.Errors[0].Code != "S3.ObjectNotFound" && attempt == attemptBudget)
                {
                    return new UploadVerificationResult(
                        UploadVerificationOutcome.VerificationFailed, objectKey,
                        Reason: head.Errors[0].Description);
                }
                // Otherwise (NotFound / transient): fall through, wait, and re-poll.
            }

            if (attempt == attemptBudget)
                break;

            var delay = TimeSpan.FromSeconds(_options.BaseDelaySeconds * Math.Pow(2, attempt - 1));
            _logger.LogInformation(
                "Upload not confirmed yet for {Bucket}/{ObjectKey} (uploadingStatus={Status}); " +
                "waiting {Delay}s before re-query (attempt {Attempt}/{Max}).",
                bucketName, objectKey, uploadingStatus, delay.TotalSeconds, attempt, attemptBudget);

            await Task.Delay(delay, ct);

            var query = await _provider.QueryAsync(resourceId, sid, ct);

            if (query.IsError)
            {
                // 404 means the recording resource has exited. No more state will come
                // from Agora — do one last S3 ground-truth check and decide.
                if (query.Errors[0].Code == "Agora.NotFound")
                {
                    var finalHead = await _storage.HeadObjectAsync(bucketName, objectKey, ct);
                    if (!finalHead.IsError && finalHead.Value.ContentLength > 0)
                    {
                        return new UploadVerificationResult(
                            UploadVerificationOutcome.Verified, objectKey, finalHead.Value);
                    }

                    _logger.LogWarning(
                        "Query returned 404 for resourceId={ResourceId}, sid={Sid} and S3 object not confirmed; leaving PendingUpload.",
                        resourceId, sid);

                    return new UploadVerificationResult(
                        UploadVerificationOutcome.Pending, objectKey,
                        Reason: "Recording resource exited before S3 upload could be confirmed.");
                }

                // Transient Query error (already retried by the HTTP policy) — try again.
                _logger.LogWarning(
                    "Query failed during verification for resourceId={ResourceId}, sid={Sid}: {Error}",
                    resourceId, sid, query.Errors[0].Description);
                continue;
            }

            uploadingStatus = query.Value.UploadingStatus;

            // Prefer the freshest object key Agora reports (mix+HLS returns the M3U8).
            var queriedKey = query.Value.Files?.FirstOrDefault()?.ObjectKey;
            if (!string.IsNullOrWhiteSpace(queriedKey))
                objectKey = queriedKey;
        }

        _logger.LogWarning(
            "Upload not confirmed within {Attempts} attempts for {Bucket}/{ObjectKey}; leaving PendingUpload.",
            attemptBudget, bucketName, objectKey);

        return new UploadVerificationResult(
            UploadVerificationOutcome.Pending, objectKey,
            Reason: "Upload not confirmed within the synchronous verification window.");
    }
}
