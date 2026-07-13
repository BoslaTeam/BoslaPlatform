using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Storage.Dtos;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage.Cloudflare;

public sealed class CloudflareR2ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;
    private readonly ILogger<CloudflareR2ObjectStorage> _logger;

    public CloudflareR2ObjectStorage(
        IOptions<StorageOptions> options,
        ILogger<CloudflareR2ObjectStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        var credentials = new BasicAWSCredentials(
            _options.AccessKey,
            _options.SecretKey);

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true,
            SignatureVersion = "4",
            UseHttp = false,
            //RetryMode = RequestRetryMode.Adaptive,
            //MaxErrorRetry = _options.MaxRetryAttempts,
            //UseChunkEncoding = false
        };

        //if (!string.IsNullOrEmpty(_options.Region))
        //{
        //    config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
        //}

        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<Result<UploadObjectResponse>> UploadAsync(
        UploadObjectRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = request.BucketName,
                Key = request.ObjectKey,
                InputStream = request.Content,
                ContentType = request.ContentType,
                Headers = { ContentLength = request.ContentLength },
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            };
            putRequest.Headers.ContentLength = request.ContentLength;
            var response = await _s3Client.PutObjectAsync(putRequest, ct);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning(
                    "Upload returned unexpected status {StatusCode} for {Bucket}/{Key}",
                    response.HttpStatusCode, request.BucketName, request.ObjectKey);

                return Error.Failure(
                    "Storage.UploadFailed",
                    $"Unexpected response status: {response.HttpStatusCode}");
            }

            _logger.LogInformation(
                "Uploaded {ContentLength} bytes to {Bucket}/{Key}, ETag={ETag}, VersionId={VersionId}",
                request.ContentLength, request.BucketName, request.ObjectKey,
                response.ETag, response.VersionId);

            return new UploadObjectResponse(
                request.BucketName,
                request.ObjectKey,
                request.ContentLength,
                DateTime.UtcNow,
                ETag: response.ETag,
                VersionId: response.VersionId);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is
            System.Net.HttpStatusCode.Forbidden or
            System.Net.HttpStatusCode.NotFound or
            System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogError(ex, "Non-retryable S3 error uploading to {Bucket}/{Key}",
                request.BucketName, request.ObjectKey);

            return Error.Failure(
                "Storage.NonRetryableError",
                $"S3 operation rejected: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Bucket}/{Key}",
                request.BucketName, request.ObjectKey);

            return Error.Failure(
                "Storage.UploadFailed",
                ex.Message);
        }
    }

    public async Task<Result<DownloadObjectResponse>> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using var response = await _s3Client.GetObjectAsync(request, ct);

            var contentStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(contentStream, ct);
            contentStream.Position = 0;

            return new DownloadObjectResponse(
                contentStream,
                response.Headers.ContentType,
                response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is
            System.Net.HttpStatusCode.Forbidden or
            System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError(ex, "Non-retryable S3 error downloading {Bucket}/{Key}",
                bucketName, objectKey);

            return Error.Failure(
                "Storage.DownloadRejected",
                $"S3 download rejected: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Bucket}/{Key}",
                bucketName, objectKey);

            return Error.Failure(
                "Storage.DownloadFailed",
                ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.DeleteObjectAsync(request, ct);

            _logger.LogInformation("Deleted {Bucket}/{Key}", bucketName, objectKey);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for {Bucket}/{Key}", bucketName, objectKey);

            return Error.Failure(
                "Storage.DeleteFailed",
                ex.Message);
        }
    }

    public async Task<Result<bool>> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await _s3Client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exists check failed for {Bucket}/{Key}", bucketName, objectKey);

            return Error.Failure(
                "Storage.ExistsCheckFailed",
                ex.Message);
        }
    }

    public async Task<Result<ObjectMetadata>> GetMetadataAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, ct);

            return new ObjectMetadata(
                bucketName,
                objectKey,
                response.Headers.ContentType,
                response.ContentLength,
                response.LastModified.ToUniversalTime(),
                response.Metadata.Keys.ToDictionary(k => k, k => response.Metadata[k]));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Error.NotFound(
                "Storage.ObjectNotFound",
                $"Object {bucketName}/{objectKey} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMetadata failed for {Bucket}/{Key}", bucketName, objectKey);

            return Error.Failure(
                "Storage.MetadataFailed",
                ex.Message);
        }
    }

    public async Task<Result<string>> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        try
        {
            var expirationTime = expiration ?? TimeSpan.FromMinutes(_options.PresignedUrlExpirationMinutes);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expirationTime),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Presigned URL generation failed for {Bucket}/{Key}",
                bucketName, objectKey);

            return Error.Failure(
                "Storage.PresignedUrlFailed",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DownloadObjectResponse>> OpenReadStreamAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            // Intentionally NOT using 'using' here.
            // The ResponseStream is the raw network stream from Cloudflare R2; it must
            // remain open until the caller has fully written the HTTP response body.
            // The caller disposes the stream, which closes the underlying HTTP connection.
            var response = await _s3Client.GetObjectAsync(request, ct);

            _logger.LogInformation(
                "Opened read stream for {Bucket}/{Key}, ContentLength={ContentLength}",
                bucketName, objectKey, response.ContentLength);

            var contentType = response.Headers.ContentType ?? "application/octet-stream";

            return new DownloadObjectResponse(
                response.ResponseStream,
                contentType,
                response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is
            System.Net.HttpStatusCode.Forbidden or
            System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError(ex, "Non-retryable S3 error opening stream for {Bucket}/{Key}",
                bucketName, objectKey);

            return Error.Failure(
                "Storage.StreamOpenRejected",
                $"S3 stream open rejected: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open stream for {Bucket}/{Key}",
                bucketName, objectKey);

            return Error.Failure(
                "Storage.StreamOpenFailed",
                ex.Message);
        }
    }

    public void Dispose()
    {
        _s3Client?.Dispose();
    }
}