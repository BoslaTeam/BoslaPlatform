// THIS IS A DEVELOPMENT-ONLY CONTROLLER
// Remove or disable before deploying to Production.
// See: docs/RecordingAccess.md for production storage patterns.

using System.Diagnostics;
using System.Text;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Interfaces.Storage.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using HttpIResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.Dev;

[ApiController]
[Route("api/dev/storage")]
[ApiExplorerSettings(IgnoreApi = true)]
public class StorageTestController : ControllerBase
{
    private readonly IObjectStorage _storage;
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<StorageTestController> _logger;

    public StorageTestController(
        IObjectStorage storage,
        IOptions<StorageOptions> options,
        IWebHostEnvironment env,
        ILogger<StorageTestController> logger)
    {
        _storage = storage;
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    [HttpPost("test-upload")]
    [EndpointSummary("Temporary endpoint to test Cloudflare R2 upload (Development only).")]
    [EndpointDescription("Uploads a small text file to Cloudflare R2 and verifies it. Only available in Development. Remove before Production.")]
    public async Task<HttpIResult> TestUpload(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return Results.NotFound();
        }

        var objectKey = $"test/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}.txt";
        var bucketName = _options.BucketName;

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            var error = Error.Validation(
                "Storage.BucketNameRequired",
                "Storage bucket name is not configured.");
            return new[] { error }.ToProblem();
        }

        var content = BuildTestContent();
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        var request = new UploadObjectRequest(
            bucketName, objectKey, stream, "text/plain", bytes.Length);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Upload Started. ObjectKey: {ObjectKey}, ContentLength: {ContentLength}",
            objectKey, bytes.Length);

        try
        {
            var uploadResult = await _storage.UploadAsync(request, ct);

            if (uploadResult.IsError)
            {
                sw.Stop();
                _logger.LogError(
                    "Upload Failed. ObjectKey: {ObjectKey}, Duration: {Duration}ms, ContentLength: {ContentLength}",
                    objectKey, sw.ElapsedMilliseconds, bytes.Length);
                return uploadResult.Errors.ToProblem();
            }

            var uploadResponse = uploadResult.Value;

            var existsResult = await _storage.ExistsAsync(bucketName, objectKey, ct);

            if (existsResult.IsError || !existsResult.Value)
            {
                sw.Stop();
                _logger.LogError(
                    "Upload Failed. ObjectKey: {ObjectKey}, Duration: {Duration}ms, ContentLength: {ContentLength}",
                    objectKey, sw.ElapsedMilliseconds, bytes.Length);
                var error = Error.Failure(
                    "Storage.VerificationFailed",
                    "Upload succeeded but verification failed: object not found in storage.");
                return new[] { error }.ToProblem();
            }

            ObjectMetadata? metadata = null;
            var metadataResult = await _storage.GetMetadataAsync(bucketName, objectKey, ct);
            if (metadataResult.IsSuccess)
            {
                metadata = metadataResult.Value;
            }

            sw.Stop();
            _logger.LogInformation(
                "Upload Completed. ObjectKey: {ObjectKey}, Duration: {Duration}ms, ContentLength: {ContentLength}",
                objectKey, sw.ElapsedMilliseconds, uploadResponse.ContentLength);

            var result = new StorageTestUploadResponse
            {
                Success = true,
                ObjectKey = uploadResponse.ObjectKey,
                ContentLength = metadata?.ContentLength ?? uploadResponse.ContentLength,
                ContentType = metadata?.ContentType ?? "text/plain",
                UploadedAtUtc = uploadResponse.UploadedAtUtc,
                StorageProvider = _options.Provider,
                ETag = uploadResponse.ETag,
                LastModifiedUtc = metadata?.LastModified
            };

            return Results.Ok(ApiResponse<StorageTestUploadResponse>.SuccessResponse(
                result,
                "Upload test completed successfully."));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Upload Failed. ObjectKey: {ObjectKey}, Duration: {Duration}ms, ContentLength: {ContentLength}",
                objectKey, sw.ElapsedMilliseconds, bytes.Length);

            var error = Error.Unexpected(
                "Storage.Unavailable",
                "Storage test upload failed unexpectedly.");
            return new[] { error }.ToProblem();
        }
    }

    private static string BuildTestContent()
    {
        return $"Hello from Bosla!\n" +
               $"Current UTC Time: {DateTime.UtcNow:O}\n" +
               $"GUID: {Guid.NewGuid()}";
    }
}

public sealed class StorageTestUploadResponse
{
    public bool Success { get; init; }
    public string ObjectKey { get; init; } = string.Empty;
    public long ContentLength { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAtUtc { get; init; }
    public string StorageProvider { get; init; } = string.Empty;
    public string? ETag { get; init; }
    public DateTime? LastModifiedUtc { get; init; }
}
