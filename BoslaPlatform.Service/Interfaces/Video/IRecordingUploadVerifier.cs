using BoslaPlatform.Application.Interfaces.Storage;

namespace BoslaPlatform.Application.Interfaces.Video;

/// <summary>
/// Confirms that a stopped recording actually landed in Amazon S3 before the
/// platform is allowed to call it Completed. Implements the Agora-recommended
/// workflow: Stop → poll Query for <c>uploadingStatus == uploaded</c> → verify
/// with S3 HeadObject. The S3 HeadObject is the ground truth; Agora's own
/// success response is never trusted on its own.
/// </summary>
public interface IRecordingUploadVerifier
{
    /// <param name="maxAttempts">
    /// Overrides the configured attempt budget. Stop-initiated completions poll
    /// (default); webhook-initiated completions pass 1 for a single immediate S3
    /// check with no backoff delay, since the webhook is itself the async signal.
    /// </param>
    Task<UploadVerificationResult> VerifyAsync(
        string resourceId,
        string sid,
        string bucketName,
        string objectKey,
        AgoraUploadingStatus initialUploadingStatus,
        int? maxAttempts = null,
        CancellationToken ct = default);
}

/// <summary>
/// The terminal decision of a verification pass. Maps 1:1 onto the DB recording
/// state the caller should persist.
/// </summary>
public enum UploadVerificationOutcome
{
    /// <summary>Object confirmed present in S3 with ContentLength &gt; 0. → Completed.</summary>
    Verified,

    /// <summary>Not confirmed within the sync window; async webhook/reconcile may still finish it. → PendingUpload.</summary>
    Pending,

    /// <summary>Agora produced no file at all (nothing was captured). → UploadFailed.</summary>
    UploadFailed,

    /// <summary>Object is missing/zero-length or S3 errored persistently. → VerificationFailed.</summary>
    VerificationFailed
}

public sealed record UploadVerificationResult(
    UploadVerificationOutcome Outcome,
    string? ObjectKey = null,
    RecordingObjectMetadata? Metadata = null,
    string? Reason = null);
