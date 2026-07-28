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

    /// <summary>
    /// Issues an S3 HeadObject to confirm the recording actually exists in the
    /// bucket and to read its authoritative metadata (size, type, ETag, ...).
    /// This is the ground-truth check the pipeline uses before marking a recording
    /// Completed — Agora reporting "uploaded" is never trusted on its own.
    /// Returns a NotFound error ("S3.ObjectNotFound") when the object is absent.
    /// </summary>
    Task<Result<RecordingObjectMetadata>> HeadObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default);
}

/// <summary>
/// Authoritative object metadata returned by an S3 HeadObject call.
/// </summary>
public sealed record RecordingObjectMetadata(
    long ContentLength,
    string? ContentType,
    string? ETag,
    DateTime? LastModifiedUtc,
    string? StorageClass);
