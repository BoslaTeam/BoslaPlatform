using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Storage;

/// <summary>
/// Verifies upload integrity by reading metadata from object storage after upload
/// and comparing ContentLength against the upload response.
///
/// Note: Cloudflare R2 (via S3 SDK) returns ETag in <see cref="UploadObjectResponse.ETag"/>.
/// If <see cref="ObjectMetadata"/> gains ETag support in the future, add the ETag check here.
/// </summary>
public sealed class RecordingIntegrityVerifier
{
    private readonly IObjectStorage _objectStorage;
    private readonly ILogger<RecordingIntegrityVerifier> _logger;

    public RecordingIntegrityVerifier(
        IObjectStorage objectStorage,
        ILogger<RecordingIntegrityVerifier> logger)
    {
        _objectStorage = objectStorage;
        _logger = logger;
    }

    /// <summary>
    /// Reads object metadata from the storage provider and verifies it against the upload response.
    /// Returns <see cref="Result.Success()"/> if all checks pass.
    /// Returns a failure <see cref="Result"/> if the object is not found or ContentLength mismatches.
    /// </summary>
    public async Task<Result> VerifyAsync(
        string bucketName,
        string objectKey,
        UploadObjectResponse uploadResponse,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Integrity verification started for {Bucket}/{Key}",
            bucketName, objectKey);

        var metadataResult = await _objectStorage.GetMetadataAsync(bucketName, objectKey, ct);

        if (metadataResult.IsError)
        {
            var errorDesc = string.Join("; ", metadataResult.Errors.Select(e => e.Description));
            _logger.LogWarning(
                "Integrity verification failed — object not found or metadata unavailable for {Bucket}/{Key}: {Error}",
                bucketName, objectKey, errorDesc);

            return Error.Failure(
                "Recording.IntegrityFailed",
                $"Object not found or metadata unavailable after upload: {errorDesc}");
        }

        var metadata = metadataResult.Value;

        // Verify ContentLength
        if (metadata.ContentLength != uploadResponse.ContentLength)
        {
            _logger.LogWarning(
                "Integrity check ContentLength mismatch for {Bucket}/{Key}: expected={Expected}, actual={Actual}",
                bucketName, objectKey, uploadResponse.ContentLength, metadata.ContentLength);

            return Error.Failure(
                "Recording.IntegrityFailed",
                $"ContentLength mismatch: expected {uploadResponse.ContentLength}, got {metadata.ContentLength}.");
        }

        // ETag cross-check: compare upload ETag against metadata-level ETag stored in custom metadata headers
        // (Cloudflare R2 returns ETag in the upload PutObject response but not in HeadObject Metadata dict
        // by default; if your bucket is configured to surface it, add the check here).
        if (!string.IsNullOrWhiteSpace(uploadResponse.ETag) &&
            metadata.Metadata != null &&
            metadata.Metadata.TryGetValue("etag", out var metaEtag) &&
            !string.IsNullOrWhiteSpace(metaEtag) &&
            !string.Equals(NormalizeETag(uploadResponse.ETag), NormalizeETag(metaEtag),
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Integrity check ETag mismatch for {Bucket}/{Key}: expected={Expected}, actual={Actual}",
                bucketName, objectKey, uploadResponse.ETag, metaEtag);

            return Error.Failure(
                "Recording.IntegrityFailed",
                $"ETag mismatch: expected {uploadResponse.ETag}, got {metaEtag}.");
        }

        _logger.LogInformation(
            "Integrity verification passed for {Bucket}/{Key}: ContentLength={ContentLength}",
            bucketName, objectKey, metadata.ContentLength);

        return Result.Success();
    }

    // S3-compatible ETags are sometimes returned with surrounding quotes — strip them.
    private static string NormalizeETag(string etag)
        => etag.Trim('"');
}
