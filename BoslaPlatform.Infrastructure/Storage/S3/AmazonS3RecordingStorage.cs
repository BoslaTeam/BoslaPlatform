using Amazon.S3;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.Storage.S3;

/// <summary>
/// Recording storage implementation backed by Amazon S3.
/// Generates short-lived presigned URLs for secure recording playback.
///
/// Agora Cloud Recording uploads directly to S3 after recording stops.
/// This service provides access to those files without exposing bucket
/// credentials or requiring the file to be streamed through the API.
/// </summary>
internal sealed class AmazonS3RecordingStorage : IRecordingStorage
{
    private readonly IAmazonS3 _s3Client;

    public AmazonS3RecordingStorage(IAmazonS3 s3Client)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    }

    public async Task<Result<string>> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiration,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return Error.Validation("S3.BucketRequired", "S3 bucket name is required.");

        if (string.IsNullOrWhiteSpace(objectKey))
            return Error.Validation("S3.ObjectKeyRequired", "S3 object key is required.");

        try
        {
            var request = new Amazon.S3.Model.GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expiration),
                Protocol = Amazon.S3.Protocol.HTTPS
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);

            if (string.IsNullOrWhiteSpace(url))
                return Error.Failure("S3.UrlGenerationFailed", "Failed to generate presigned URL.");

            return Result<string>.Success(url);
        }
        catch (AmazonS3Exception ex)
        {
            return Error.Failure(
                "S3.PresignedUrlFailed",
                $"Failed to generate presigned URL for {bucketName}/{objectKey}: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return Error.Validation("S3.BucketRequired", "S3 bucket name is required.");

        if (string.IsNullOrWhiteSpace(objectKey))
            return Error.Validation("S3.ObjectKeyRequired", "S3 object key is required.");

        try
        {
            var request = new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request, ct);

            return Result<Stream>.Success(response.ResponseStream);
        }
        catch (AmazonS3Exception ex)
        {
            return Error.Failure(
                "S3.StreamFailed",
                $"Failed to open S3 stream for {bucketName}/{objectKey}: {ex.Message}");
        }
    }
}
