using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application.Settings;

/// <summary>
/// Controls the synchronous "confirm the recording actually reached S3" step that
/// runs after Agora's Stop returns. Agora returning HTTP 200 only means the stop
/// was accepted — it does NOT mean the upload finished — so the pipeline polls
/// Query for <c>uploadingStatus == uploaded</c> and then verifies with S3
/// HeadObject before a recording is allowed to become Completed.
/// </summary>
public sealed class RecordingUploadVerificationOptions
{
    public const string SectionName = "RecordingUploadVerification";

    /// <summary>
    /// Maximum number of Query/HeadObject attempts before the sync verification
    /// gives up and leaves the recording PendingUpload for the async webhook /
    /// reconciliation to finalize. Default: 4.
    /// </summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 4;

    /// <summary>
    /// Base delay in seconds for exponential backoff between attempts.
    /// Attempt N waits <c>BaseDelaySeconds * 2^(N-1)</c> (1s, 2s, 4s, ...),
    /// matching Agora's recommended back-off strategy. Default: 1.
    /// </summary>
    [Range(1, 60)]
    public int BaseDelaySeconds { get; set; } = 1;
}
