using System.Net;
using System.Text;
using System.Text.Json;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Bosla.Unit.Tests
{
    /// <summary>
    /// Asserts that the pipeline actually emits its stage events. Observability
    /// that is never exercised is indistinguishable from observability that does
    /// not work — these tests are what keep the timeline trustworthy.
    /// </summary>
    public class RecordingPipelineObservabilityTests
    {
        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static AgoraSettings Settings() => new()
        {
            AppId = "fedcba9876543210fedcba9876543210",
            AppCertificate = "0123456789abcdef0123456789abcdef",
            CloudRecordingBaseUrl = "https://api.agora.io",
            RecordingMode = RecordingMode.Mix,
            RecordingStreamTypes = 2,
            StorageBucket = "test-bucket"
        };

        private static AgoraRecordingProvider Provider(
            HttpMessageHandler handler, IRecordingPipelineLog pipelineLog)
        {
            var settings = Settings();
            var client = new AgoraCloudRecordingApiClient(
                new HttpClient(handler),
                Options.Create(settings),
                Options.Create(new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions()),
                Mock.Of<ILogger<AgoraCloudRecordingApiClient>>());

            return new AgoraRecordingProvider(
                client, Options.Create(settings),
                Mock.Of<ILogger<AgoraRecordingProvider>>(), pipelineLog);
        }

        private static Mock<HttpMessageHandler> Handler(params (HttpStatusCode Status, object Body)[] responses)
        {
            var call = 0;
            var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    var (status, body) = responses[Math.Min(call++, responses.Length - 1)];
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = status,
                        Content = new StringContent(
                            JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
                    });
                });
            return mock;
        }

        [Fact]
        public async Task Successful_start_emits_Acquire_and_Start_stages_with_correlatable_identity()
        {
            var log = new Mock<IRecordingPipelineLog>();

            var handler = Handler(
                (HttpStatusCode.OK, new { resourceId = "resource-123" }),
                (HttpStatusCode.OK, new { sid = "sid-456", resourceId = "resource-123" }));

            var result = await Provider(handler.Object, log.Object)
                .StartRecordingAsync("bosla-channel");

            Assert.False(result.IsError);

            log.Verify(l => l.Started(RecordingStage.Acquire, It.IsAny<RecordingLogContext>()), Times.Once);
            log.Verify(l => l.Started(RecordingStage.Start, It.IsAny<RecordingLogContext>()), Times.Once);

            // Acquire succeeds before a SID exists, so it correlates on the channel.
            // Start carries the SID — the timeline store uses it as a JOIN KEY to
            // stitch this provider stage onto the canonical rec-{id} timeline; the
            // SID is deliberately not the canonical id itself.
            log.Verify(l => l.Succeeded(
                    RecordingStage.Acquire,
                    It.Is<RecordingLogContext>(c => c.ResourceId == "resource-123"),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<IReadOnlyDictionary<string, object?>>()),
                Times.Once);

            log.Verify(l => l.Succeeded(
                    RecordingStage.Start,
                    It.Is<RecordingLogContext>(c =>
                        c.Sid == "sid-456" &&
                        c.ResourceId == "resource-123" &&
                        c.CorrelationId == "sid-sid-456"),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<IReadOnlyDictionary<string, object?>>()),
                Times.Once);
        }

        [Fact]
        public async Task Failed_acquire_emits_a_stage_failure_carrying_the_error_code()
        {
            var log = new Mock<IRecordingPipelineLog>();
            var handler = Handler((HttpStatusCode.Unauthorized, new { error = "bad creds" }));

            var result = await Provider(handler.Object, log.Object)
                .StartRecordingAsync("bosla-channel");

            Assert.True(result.IsError);

            log.Verify(l => l.Failed(
                    RecordingStage.Acquire,
                    It.IsAny<RecordingLogContext>(),
                    "Agora.Unauthorized",
                    It.IsAny<string>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);

            // Start must never be reported as attempted when Acquire failed.
            log.Verify(l => l.Started(RecordingStage.Start, It.IsAny<RecordingLogContext>()), Times.Never);
        }

        [Fact]
        public async Task Stop_that_returns_no_files_is_reported_as_a_stage_failure()
        {
            // The defect that started this incident: HTTP 200 with an empty
            // fileList. It must surface as a failure, not a silent success.
            var log = new Mock<IRecordingPipelineLog>();
            var handler = Handler((HttpStatusCode.OK, new
            {
                resourceId = "resource-123",
                sid = "sid-456",
                serverResponse = new { status = "stopped", fileList = Array.Empty<object>() }
            }));

            var result = await Provider(handler.Object, log.Object)
                .StopRecordingAsync("bosla-channel", "resource-123", "sid-456", "1279316212");

            Assert.False(result.IsError);

            log.Verify(l => l.Failed(
                    RecordingStage.Stop,
                    It.Is<RecordingLogContext>(c => c.CorrelationId == "sid-sid-456"),
                    It.Is<string>(code => code.Contains("NoFiles") || code.Contains("NoServerResponse")),
                    It.IsAny<string>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
        }
    }
}
