using System.Net;
using System.Text;
using System.Text.Json;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BoslaPlatform.Application.Observability;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bosla.Unit.Tests;

public class AgoraRecordingProviderIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static AgoraSettings CreateValidSettings()
    {
        return new AgoraSettings
        {
            AppId = "test-app-id",
            CustomerId = "test-customer-id",
            CustomerSecret = "test-customer-secret",
            CloudRecordingBaseUrl = "https://api.agora.io/v1/apps/test-app-id/cloud_recording",
            RecordingMaxIdleTime = 120,
            RecordingStreamTypes = 0,
            StorageVendor = 1,
            StorageRegion = 0,
            StorageBucket = "test-bucket",
            StorageAccessKey = "test-access-key",
            StorageSecretKey = "test-secret-key",
            TimeoutSeconds = 10,
            RetryCount = 1
        };
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(
        HttpStatusCode statusCode,
        object? responseBody = null)
    {
        var content = responseBody is not null
            ? JsonSerializer.Serialize(responseBody, JsonOpts)
            : "{}";

        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return mock;
    }

    private static AgoraRecordingProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.agora.io")
        };

        var client = new AgoraCloudRecordingApiClient(
            httpClient,
            Options.Create(CreateValidSettings()),
            Options.Create(new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions()),
            Mock.Of<ILogger<AgoraCloudRecordingApiClient>>());

        return new AgoraRecordingProvider(
            client,
            Options.Create(CreateValidSettings()),
            Mock.Of<ILogger<AgoraRecordingProvider>>(),
                Mock.Of<IRecordingPipelineLog>());
    }

    // ──────────────────────────────────────────────
    // AcquireAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AcquireAsync_returns_resource_id_on_success()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, new { resourceId = "abc-123" });
        var provider = CreateProvider(handler.Object);

        var result = await provider.AcquireAsync("channel-test");

        Assert.False(result.IsError);
        Assert.Equal("abc-123", result.Value.ResourceId);
    }

    [Fact]
    public async Task AcquireAsync_returns_error_on_missing_resourceId()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, new { });
        var provider = CreateProvider(handler.Object);

        var result = await provider.AcquireAsync("channel-test");

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task AcquireAsync_returns_error_on_empty_channel()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, new { resourceId = "abc" });
        var provider = CreateProvider(handler.Object);

        var result = await provider.AcquireAsync("");

        Assert.True(result.IsError);
        Assert.Contains("MissingChannelName", result.Errors[0].Code);
    }

    // ──────────────────────────────────────────────
    // StartRecordingAsync — end-to-end Acquire + Start
    // ──────────────────────────────────────────────

    [Fact]
    public async Task StartRecordingAsync_acquire_then_start_returns_sid()
    {
        var callCount = 0;

        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                object body;
                if (callCount == 1)
                {
                    body = new { resourceId = "resource-1" };
                }
                else
                {
                    body = new { sid = "sid-1", resourceId = "resource-1" };
                }

                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(body, JsonOpts),
                        Encoding.UTF8,
                        "application/json")
                });
            });

        var provider = CreateProvider(mock.Object);
        var result = await provider.StartRecordingAsync("channel-test");

        Assert.False(result.IsError);
        Assert.Equal("resource-1", result.Value.ProviderRecordingId);
        Assert.Equal("sid-1", result.Value.ProviderRecordingSid);
        Assert.NotEmpty(result.Value.RecordingUid);
    }

    [Fact]
    public async Task StartRecordingAsync_acquire_fails_returns_error()
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"bad request\"}",
                    Encoding.UTF8, "application/json")
            });

        var provider = CreateProvider(mock.Object);

        var result = await provider.StartRecordingAsync("channel-test");

        Assert.True(result.IsError);
        Assert.Contains("BadRequest", result.Errors[0].Code);
    }

    // ──────────────────────────────────────────────
    // QueryAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_returns_status_and_files()
    {
        var agoraResponse = new
        {
            resourceId = "resource-1",
            sid = "sid-1",
            serverResponse = new
            {
                status = "stopped",
                fileList = new[]
                {
                    new { fileName = "test.m3u8", fileSize = 1024L, sliceStartTime = 1000000L }
                }
            }
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, agoraResponse);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.False(result.IsError);
        Assert.Equal(RecordingStatus.Completed, result.Value.Status);
        Assert.Equal("resource-1", result.Value.ResourceId);
        Assert.Equal("sid-1", result.Value.Sid);
        Assert.NotNull(result.Value.Files);
        Assert.Single(result.Value.Files);
        Assert.Equal("test.m3u8", result.Value.Files[0].FileName);
        Assert.Equal("application/vnd.apple.mpegurl", result.Value.Files[0].MimeType);
        Assert.NotNull(result.Value.Summary);
        Assert.Equal(1, result.Value.Summary.FileCount);
        Assert.Equal(1024L, result.Value.Summary.TotalSizeBytes);
    }

    [Fact]
    public async Task QueryAsync_maps_agora_inProgress_status()
    {
        var agoraResponse = new
        {
            serverResponse = new { status = "inProgress" }
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, agoraResponse);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.False(result.IsError);
        Assert.Equal(RecordingStatus.Processing, result.Value.Status);
    }

    [Fact]
    public async Task QueryAsync_maps_agora_failed_status()
    {
        var agoraResponse = new
        {
            serverResponse = new { status = "failed" }
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, agoraResponse);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.False(result.IsError);
        Assert.Equal(RecordingStatus.Failed, result.Value.Status);
    }

    // ──────────────────────────────────────────────
    // StopRecordingAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task StopRecordingAsync_returns_file_list()
    {
        var agoraResponse = new
        {
            resourceId = "resource-1",
            sid = "sid-1",
            serverResponse = new
            {
                status = "stopped",
                fileList = new[]
                {
                    new { fileName = "clip1.m3u8", fileSize = 2048L, sliceStartTime = 2000000L },
                    new { fileName = "clip2.ts", fileSize = 4096L, sliceStartTime = 3000000L }
                }
            }
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, agoraResponse);
        var provider = CreateProvider(handler.Object);

        var result = await provider.StopRecordingAsync("channel-test", "resource-1", "sid-1", "123456789");

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Files);
        Assert.Equal(2, result.Value.Files.Count);
        Assert.Equal("clip1.m3u8", result.Value.Files[0].FileName);
        Assert.Equal("clip2.ts", result.Value.Files[1].FileName);
        Assert.NotNull(result.Value.Summary);
        Assert.Equal(2, result.Value.Summary.FileCount);
        Assert.Equal(6144L, result.Value.Summary.TotalSizeBytes);
    }

    // ──────────────────────────────────────────────
    // Status Mapping (tested via provider)
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("inProgress", RecordingStatus.Processing)]
    [InlineData("processing", RecordingStatus.Processing)]
    [InlineData("stopped", RecordingStatus.Completed)]
    [InlineData("completed", RecordingStatus.Completed)]
    [InlineData("failed", RecordingStatus.Failed)]
    [InlineData("idle", RecordingStatus.Idle)]
    [InlineData("uploading", RecordingStatus.Uploading)]
    [InlineData("uploaded", RecordingStatus.Uploaded)]
    [InlineData("starting", RecordingStatus.Starting)]
    [InlineData("cancelled", RecordingStatus.Cancelled)]
    [InlineData("unknown", RecordingStatus.Processing)]
    public async Task QueryAsync_maps_all_agora_statuses(string agoraStatus, RecordingStatus expected)
    {
        var agoraResponse = new
        {
            serverResponse = new { status = agoraStatus }
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, agoraResponse);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value.Status);
    }

    // ──────────────────────────────────────────────
    // Error Responses
    // ──────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_returns_unauthorized_on_401()
    {
        var handler = CreateMockHandler(HttpStatusCode.Unauthorized);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.True(result.IsError);
        Assert.Contains("Unauthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task QueryAsync_returns_error_on_404()
    {
        var handler = CreateMockHandler(HttpStatusCode.NotFound);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.True(result.IsError);
        Assert.Contains("NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task QueryAsync_returns_rate_limited_on_429()
    {
        var handler = CreateMockHandler(HttpStatusCode.TooManyRequests);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.True(result.IsError);
        Assert.Contains("RateLimited", result.Errors[0].Code);
    }

    [Fact]
    public async Task QueryAsync_returns_server_error_on_500()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError);
        var provider = CreateProvider(handler.Object);

        var result = await provider.QueryAsync("resource-1", "sid-1");

        Assert.True(result.IsError);
        Assert.Contains("ServerError", result.Errors[0].Code);
    }

    [Fact]
    public async Task AcquireAsync_returns_validation_error_on_missing_app_id()
    {
        var settings = CreateValidSettings();
        settings.AppId = string.Empty;

        var client = new AgoraCloudRecordingApiClient(
            new HttpClient(new Mock<HttpMessageHandler>().Object),
            Options.Create(settings),
            Options.Create(new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions()),
            Mock.Of<ILogger<AgoraCloudRecordingApiClient>>());

        var provider = new AgoraRecordingProvider(
            client,
            Options.Create(settings),
            Mock.Of<ILogger<AgoraRecordingProvider>>(),
                Mock.Of<IRecordingPipelineLog>());

        var result = await provider.AcquireAsync("channel-test");

        Assert.True(result.IsError);
        Assert.Contains("AppIdMissing", result.Errors[0].Code);
    }
}