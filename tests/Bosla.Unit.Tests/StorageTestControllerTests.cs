using System.Text;
using System.Text.RegularExpressions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.API.Controllers.Dev;
using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class StorageTestControllerTests
{
    private readonly Mock<IObjectStorage> _storageMock = new();
    private readonly Mock<IWebHostEnvironment> _envMock = new();
    private readonly StorageOptions _options = new()
    {
        Provider = "CloudflareR2",
        BucketName = "test-bucket",
        AccessKey = "not-used",
        SecretKey = "not-used",
        ServiceUrl = "https://example.r2.cloudflarestorage.com"
    };

    [Fact]
    public async Task TestUpload_success_uploads_memory_stream_and_returns_response()
    {
        UseEnvironment(Environments.Development);
        UploadObjectRequest? capturedRequest = null;
        string? capturedContent = null;

        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (UploadObjectRequest request, CancellationToken _) =>
            {
                capturedRequest = request;
                using var reader = new StreamReader(
                    request.Content,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                capturedContent = await reader.ReadToEndAsync();

                return new UploadObjectResponse(
                    request.BucketName,
                    request.ObjectKey,
                    request.ContentLength,
                    DateTime.UtcNow,
                    ETag: "\"etag-1\"");
            });

        _storageMock.Setup(x => x.ExistsAsync(
                _options.BucketName,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _storageMock.Setup(x => x.GetMetadataAsync(
                _options.BucketName,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string bucket, string key, CancellationToken _) =>
                new ObjectMetadata(bucket, key, "text/plain", capturedRequest!.ContentLength, DateTime.UtcNow));

        var controller = CreateController();

        var result = await controller.TestUpload(CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var response = Assert.IsType<ApiResponse<StorageTestUploadResponse>>(valueResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.True(data.Success);
        Assert.Equal("CloudflareR2", data.StorageProvider);
        Assert.Equal("text/plain", data.ContentType);
        Assert.Equal("\"etag-1\"", data.ETag);
        Assert.Matches(new Regex("^test/[0-9]{4}/[0-9]{2}/[0-9]{2}/[a-f0-9]{32}\\.txt$"), data.ObjectKey);

        Assert.NotNull(capturedRequest);
        Assert.IsType<MemoryStream>(capturedRequest!.Content);
        Assert.Equal(_options.BucketName, capturedRequest.BucketName);
        Assert.Equal("text/plain", capturedRequest.ContentType);
        Assert.True(capturedRequest.ContentLength > 0);
        Assert.Contains("Hello from Bosla!", capturedContent);
        Assert.Contains("Current UTC Time:", capturedContent);
        Assert.Contains("GUID:", capturedContent);

        _storageMock.Verify(x => x.ExistsAsync(
            _options.BucketName,
            capturedRequest.ObjectKey,
            It.IsAny<CancellationToken>()), Times.Once);
        _storageMock.Verify(x => x.GetMetadataAsync(
            _options.BucketName,
            capturedRequest.ObjectKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestUpload_upload_failure_returns_failure_result()
    {
        UseEnvironment(Environments.Development);
        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Storage.UploadFailed", "Upload failed."));

        var controller = CreateController();

        var result = await controller.TestUpload(CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        _storageMock.Verify(x => x.ExistsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestUpload_exists_false_returns_failure_result()
    {
        UseEnvironment(Environments.Development);
        _storageMock.Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UploadObjectRequest request, CancellationToken _) =>
                new UploadObjectResponse(
                    request.BucketName,
                    request.ObjectKey,
                    request.ContentLength,
                    DateTime.UtcNow));

        _storageMock.Setup(x => x.ExistsAsync(
                _options.BucketName,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController();

        var result = await controller.TestUpload(CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        _storageMock.Verify(x => x.GetMetadataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestUpload_outside_development_returns_not_found()
    {
        UseEnvironment(Environments.Production);
        var controller = CreateController();

        var result = await controller.TestUpload(CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
        _storageMock.Verify(x => x.UploadAsync(
            It.IsAny<UploadObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private StorageTestController CreateController()
    {
        return new StorageTestController(
            _storageMock.Object,
            Options.Create(_options),
            _envMock.Object,
            Mock.Of<ILogger<StorageTestController>>());
    }

    private void UseEnvironment(string environmentName)
    {
        _envMock.SetupGet(x => x.EnvironmentName).Returns(environmentName);
    }
}
