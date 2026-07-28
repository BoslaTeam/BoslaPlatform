using System.Net;
using System.Text;
using System.Text.Json;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BoslaPlatform.Application.Observability;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bosla.Unit.Tests
{
    public class AgoraRecordingUidFlowTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, object responseBody)
        {
            var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(
                        JsonSerializer.Serialize(responseBody, JsonOpts),
                        Encoding.UTF8,
                        "application/json")
                });
            return mock;
        }

        private AgoraRecordingProvider CreateProvider(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var settings = new AgoraSettings
            {
                AppId = "test-app-id",
                CloudRecordingBaseUrl = "https://api.agora.io",
                RecordingMode = RecordingMode.Individual,
                RecordingMaxIdleTime = 30,
                RecordingStreamTypes = 2,
                StorageVendor = 1,
                StorageRegion = 1,
                StorageBucket = "test-bucket",
                StorageAccessKey = "test-access-key",
                StorageSecretKey = "test-secret-key",
                StorageFileNamePrefix = "prefix"
            };

            var client = new AgoraCloudRecordingApiClient(
                httpClient,
                Options.Create(settings),
                Options.Create(new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions()),
                Mock.Of<ILogger<AgoraCloudRecordingApiClient>>());

            return new AgoraRecordingProvider(
                client,
                Options.Create(settings),
                Mock.Of<ILogger<AgoraRecordingProvider>>(),
                Mock.Of<IRecordingPipelineLog>());
        }

        [Fact]
        public async Task StartRecordingAsync_generates_uid_once_and_uses_it_for_acquire_and_start()
        {
            // Arrange
            var callCount = 0;
            string? acquireUid = null;
            string? startUid = null;

            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    callCount++;
                    object responseBody;
                    var contentTask = request.Content?.ReadAsStringAsync(ct) ?? Task.FromResult("");
                    contentTask.Wait(ct);
                    var bodyString = contentTask.Result;

                    if (callCount == 1)
                    {
                        // Acquire request
                        using var doc = JsonDocument.Parse(bodyString);
                        acquireUid = doc.RootElement.GetProperty("uid").GetString();
                        responseBody = new { resourceId = "resource-123" };
                    }
                    else
                    {
                        // Start request
                        using var doc = JsonDocument.Parse(bodyString);
                        startUid = doc.RootElement.GetProperty("uid").GetString();
                        responseBody = new { sid = "sid-456", resourceId = "resource-123" };
                    }

                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(responseBody, JsonOpts),
                            Encoding.UTF8,
                            "application/json")
                    });
                });

            var provider = CreateProvider(mockHandler.Object);

            // Act
            var result = await provider.StartRecordingAsync("test-channel");

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(acquireUid);
            Assert.NotNull(startUid);
            Assert.Equal(acquireUid, startUid); // Identical UID used for Acquire and Start
            Assert.Equal("resource-123", result.Value.ProviderRecordingId);
            Assert.Equal("sid-456", result.Value.ProviderRecordingSid);
            Assert.Equal(acquireUid, result.Value.RecordingUid); // Returned in result
        }

        [Fact]
        public async Task StartRecordingAsync_subscribes_to_all_streams()
        {
            // Arrange
            var callCount = 0;
            string? startBody = null;

            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    callCount++;
                    var contentTask = request.Content?.ReadAsStringAsync(ct) ?? Task.FromResult("");
                    contentTask.Wait(ct);

                    object responseBody;
                    if (callCount == 1)
                    {
                        responseBody = new { resourceId = "resource-123" };
                    }
                    else
                    {
                        startBody = contentTask.Result;
                        responseBody = new { sid = "sid-456", resourceId = "resource-123" };
                    }

                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(responseBody, JsonOpts),
                            Encoding.UTF8,
                            "application/json")
                    });
                });

            var provider = CreateProvider(mockHandler.Object);

            // Act
            var result = await provider.StartRecordingAsync("test-channel");

            // Assert — without the subscribe lists Agora records nothing and the
            // recorder is torn down once maxIdleTime elapses.
            Assert.False(result.IsError);
            Assert.NotNull(startBody);

            using var doc = JsonDocument.Parse(startBody!);
            var recordingConfig = doc.RootElement
                .GetProperty("clientRequest")
                .GetProperty("recordingConfig");

            Assert.Equal(
                "#allstream#",
                recordingConfig.GetProperty("subscribeAudioUids")[0].GetString());
            Assert.Equal(
                "#allstream#",
                recordingConfig.GetProperty("subscribeVideoUids")[0].GetString());
        }

        [Fact]
        public async Task StartRecordingAsync_refuses_to_start_when_token_generation_yields_empty_token()
        {
            // Arrange — RtcTokenBuilder2 returns "" (it does not throw) for a
            // malformed App Certificate. Starting anyway would join the channel
            // with no token and silently record nothing.
            var httpClient = new HttpClient(new Mock<HttpMessageHandler>(MockBehavior.Strict).Object);
            var settings = new AgoraSettings
            {
                AppId = "test-app-id",
                AppCertificate = "not-a-valid-certificate",
                CloudRecordingBaseUrl = "https://api.agora.io",
                RecordingMode = RecordingMode.Mix,
                RecordingStreamTypes = 2,
                StorageBucket = "test-bucket"
            };

            var client = new AgoraCloudRecordingApiClient(
                httpClient,
                Options.Create(settings),
                Options.Create(new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions()),
                Mock.Of<ILogger<AgoraCloudRecordingApiClient>>());

            var provider = new AgoraRecordingProvider(
                client, Options.Create(settings), Mock.Of<ILogger<AgoraRecordingProvider>>(),
                Mock.Of<IRecordingPipelineLog>());

            // Act
            var result = await provider.StartRecordingAsync("test-channel");

            // Assert — fails before any HTTP call (the strict handler would throw).
            Assert.True(result.IsError);
            Assert.Equal("Agora.Provider.TokenGenerationFailed", result.Errors[0].Code);
        }

        [Fact]
        public async Task StopRecordingAsync_uses_persisted_uid_and_never_sid_or_resourceid()
        {
            // Arrange
            string? stopUidSent = null;
            var stopResponse = new
            {
                resourceId = "resource-123",
                sid = "sid-456",
                serverResponse = new
                {
                    status = "stopped",
                    fileList = new[]
                    {
                        new { fileName = "file.mp4", fileSize = 100L, sliceStartTime = 12345L }
                    }
                }
            };

            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    var contentTask = request.Content?.ReadAsStringAsync(ct) ?? Task.FromResult("");
                    contentTask.Wait(ct);
                    var bodyString = contentTask.Result;

                    using var doc = JsonDocument.Parse(bodyString);
                    stopUidSent = doc.RootElement.GetProperty("uid").GetString();

                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(stopResponse, JsonOpts),
                            Encoding.UTF8,
                            "application/json")
                    });
                });

            var provider = CreateProvider(mockHandler.Object);
            var expectedUid = "987654321";

            // Act
            var result = await provider.StopRecordingAsync("test-channel", "resource-123", "sid-456", expectedUid);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(expectedUid, stopUidSent); // Uid sent matches the persisted recordingUid
            Assert.NotEqual("sid-456", stopUidSent); // SID is never used as UID
            Assert.NotEqual("resource-123", stopUidSent); // ResourceId is never used as UID
        }

        [Fact]
        public async Task StopRecordingAsync_fails_when_any_identifier_is_missing()
        {
            // Arrange
            var provider = CreateProvider(new Mock<HttpMessageHandler>(MockBehavior.Strict).Object);

            // Act
            var resultMissingChannel = await provider.StopRecordingAsync("", "resource-123", "sid-456", "uid-789");
            var resultMissingResourceId = await provider.StopRecordingAsync("channel", "", "sid-456", "uid-789");
            var resultMissingSid = await provider.StopRecordingAsync("channel", "resource-123", "", "uid-789");
            var resultMissingUid = await provider.StopRecordingAsync("channel", "resource-123", "sid-456", "");

            // Assert
            Assert.True(resultMissingChannel.IsError);
            Assert.True(resultMissingResourceId.IsError);
            Assert.True(resultMissingSid.IsError);
            Assert.True(resultMissingUid.IsError);
        }

        [Fact]
        public void VideoSession_and_ScreenRecording_persist_uid_when_assigned()
        {
            // Arrange
            var sessionResult = VideoSession.Create(Guid.NewGuid(), "channel-test", "app-id", VideoSessionType.OneToOne);
            var session = sessionResult.Value;
            var recordingResult = ScreenRecording.Create(session.Id, RecordingAccessControl.Both, RecordingStorageProvider.Agora);
            var recording = recordingResult.Value;

            var uid = "741852963";
            var resourceId = "resource-123";
            var sid = "sid-456";

            // Act
            session.SetRecording(resourceId, sid, uid);
            recording.AttachAgoraRecording(resourceId, sid, uid);

            // Assert
            Assert.Equal(uid, session.AgoraRecordingUid);
            Assert.Equal(uid, recording.AgoraRecordingUid);
            Assert.Equal(resourceId, session.AgoraRecordingId);
            Assert.Equal(sid, session.AgoraRecordingSid);
            Assert.Equal(resourceId, recording.AgoraRecordingId);
            Assert.Equal(sid, recording.AgoraRecordingSid);
        }
    }
}
