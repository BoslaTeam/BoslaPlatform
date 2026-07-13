using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Storage;

/// <summary>
/// Recording-specific storage abstraction for interacting with recording
/// files stored in Amazon S3 (via Agora Cloud Recording's direct S3 upload).
///
/// This interface is intentionally recording-specific and does NOT reuse
/// the generic IObjectStorage. This preserves Clean Architecture by keeping
/// storage concerns separate from generic file uploads.
/// </summary>
public interface IRecordingStorage
{
    /// <summary>
    /// Generates a short-lived presigned URL for viewing or downloading a recording.
    /// </summary>
    Task<Result<string>> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiration,
        CancellationToken ct = default);

    /// <summary>
    /// Opens a lazy read stream for the recording file from S3 without buffering
    /// the entire object into memory. The returned stream reads directly from S3
    /// over the network; the caller must dispose it.
    /// </summary>
    Task<Result<Stream>> OpenReadStreamAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);
}
