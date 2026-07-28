using System.Text.Json;
using BoslaPlatform.Application.Features.VideoSessions.Constants;
using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Shared;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests
{
    /// <summary>
    /// Replays Agora webhook payloads through the real webhook service and asserts
    /// that the recording handlers actually run and move aggregate state.
    ///
    /// PROVENANCE OF THE FIXTURES — READ BEFORE TRUSTING THESE TESTS:
    ///   The payload bodies below are DERIVED FROM AGORA'S DOCUMENTATION, not
    ///   captured from a live webhook. They prove our routing and handlers behave
    ///   correctly *given* this payload shape; they do NOT prove Agora sends this
    ///   shape. Replace each fixture with a verbatim body from the
    ///   "[AgoraWebhook] RAW" log line as soon as a real event is captured.
    /// </summary>
    public class AgoraWebhookReplayTests : IDisposable
    {
        private const string Channel = "bosla-appointment-replay-test";
        private const string Bucket = "bosla-recordings-test";
        private readonly AppDbContext _context;
        private readonly VideoSessionWebhookService _service;

        public AgoraWebhookReplayTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"WebhookReplay_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);

            // The webhook stopped/uploaded path routes through the shared completion
            // pipeline. Build a real one with a mocked S3 HeadObject that confirms the
            // object, so an "uploaded" webhook produces a verified Completed recording.
            var provider = new Mock<IRecordingProvider>();

            var storage = new Mock<IRecordingStorage>();
            storage
                .Setup(s => s.HeadObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RecordingObjectMetadata>.Success(
                    new RecordingObjectMetadata(1234, "application/vnd.apple.mpegurl", "\"etag\"", DateTime.UtcNow, "STANDARD")));

            var storageSettings = new Mock<IRecordingStorageSettings>();
            storageSettings.SetupGet(s => s.RecordingBucketName).Returns(Bucket);

            var verifier = new RecordingUploadVerifier(
                provider.Object, storage.Object,
                Options.Create(new RecordingUploadVerificationOptions { MaxAttempts = 1, BaseDelaySeconds = 1 }),
                Mock.Of<ILogger<RecordingUploadVerifier>>());

            var completion = new RecordingCompletionService(
                _context, provider.Object, verifier, storageSettings.Object,
                Mock.Of<IRecordingPipelineLog>(), Mock.Of<ILogger<RecordingCompletionService>>());

            _service = new VideoSessionWebhookService(
                _context, Mock.Of<ILogger<VideoSessionWebhookService>>(),
                Mock.Of<IRecordingPipelineLog>(), completion);
        }

        public void Dispose() => _context.Dispose();

        private static AgoraWebhookRequest Parse(string body) =>
            JsonSerializer.Deserialize<AgoraWebhookRequest>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        /// <summary>
        /// Seeds a session with no recording state at all, so that any recording
        /// state observed afterwards can only have come from a webhook handler.
        /// </summary>
        private async Task<VideoSession> SeedSessionAsync()
        {
            var session = VideoSession.Create(
                Guid.NewGuid(), Channel, "app-id", VideoSessionType.OneToOne).Value;

            _context.VideoSessions.Add(session);
            await _context.SaveChangesAsync();

            Assert.Null(session.RecordingStatus);
            return session;
        }

        // ── eventType 40 — recorder_started (productId 3) ──────────────────
        private const string RecorderStartedBody = """
        {
          "noticeId": "2ceb-1111-4a1e-8b2c-000000000040",
          "productId": 3,
          "eventType": 40,
          "notifyMs": 1768500000000,
          "payload": {
            "cname": "bosla-appointment-replay-test",
            "uid": "1279316212",
            "sid": "sid-xyz",
            "sequence": 0,
            "sendts": 1768500000,
            "details": { "msgName": "recorder_started", "resourceId": "resource-abc", "sid": "sid-xyz" }
          }
        }
        """;

        // ── eventType 31 — uploaded (productId 3) ──────────────────────────
        private const string UploadedBody = """
        {
          "noticeId": "2ceb-1111-4a1e-8b2c-000000000031",
          "productId": 3,
          "eventType": 31,
          "notifyMs": 1768500600000,
          "payload": {
            "cname": "bosla-appointment-replay-test",
            "uid": "1279316212",
            "sid": "sid-xyz",
            "details": {
              "msgName": "uploaded",
              "resourceId": "resource-abc",
              "sid": "sid-xyz",
              "fileUrl": "recordings/sid-xyz_bosla-appointment-replay-test.m3u8",
              "duration": 312,
              "fileSize": 8421376
            }
          }
        }
        """;

        [Fact]
        public async Task Replaying_eventType_40_executes_HandleRecordingStarted()
        {
            await SeedSessionAsync();

            var request = Parse(RecorderStartedBody);
            Assert.Equal(ProductIds.CloudRecording, request.ProductId);
            Assert.Equal(AgoraEventTypes.RecordingStarted, request.EventType);

            var result = await _service.ProcessAsync(request, CancellationToken.None);

            Assert.False(result.IsError);

            // Proof the handler ran rather than falling through to HandleUnknownEvent:
            // the seeded session had no recording state, and only
            // ConfirmRecordingStarted writes these three fields.
            var session = await _context.VideoSessions
                .FirstAsync(s => s.AgoraChannelName == Channel);
            Assert.Equal(RecordingStatus.Recording, session.RecordingStatus);
            Assert.Equal("resource-abc", session.AgoraRecordingId);
            Assert.Equal("sid-xyz", session.AgoraRecordingSid);
            Assert.NotNull(session.RecordingStartedAtUtc);
        }

        [Fact]
        public async Task Replaying_eventType_31_executes_uploaded_handler_and_records_the_file()
        {
            await SeedSessionAsync();
            await _service.ProcessAsync(Parse(RecorderStartedBody), CancellationToken.None);

            var request = Parse(UploadedBody);
            Assert.Equal(ProductIds.CloudRecording, request.ProductId);
            Assert.Equal(AgoraEventTypes.RecordingUploaded, request.EventType);

            var result = await _service.ProcessAsync(request, CancellationToken.None);

            Assert.False(result.IsError);

            var session = await _context.VideoSessions
                .FirstAsync(s => s.AgoraChannelName == Channel);

            // The uploaded webhook ran the shared completion pipeline: Stop is skipped
            // (WebhookConfirmed), the object is HeadObject-verified, and S3 metadata is
            // persisted — so the recording is Completed AND watchable (IsUploadedToS3).
            Assert.NotNull(session.RecordingCompletedAt);
            Assert.Equal(RecordingStatus.Completed, session.RecordingStatus);
            Assert.Equal(
                "recordings/sid-xyz_bosla-appointment-replay-test.m3u8",
                session.RecordingUrl);
            Assert.Equal(
                "recordings/sid-xyz_bosla-appointment-replay-test.m3u8",
                session.ObjectKey);
            Assert.Equal(Bucket, session.BucketName);
            Assert.True(session.IsUploadedToS3);
        }

        [Fact]
        public async Task The_previously_configured_codes_1001_and_1004_are_NOT_routed()
        {
            // Regression guard for the original defect: these codes were wired to
            // the recording handlers but Agora never sends them for cloud recording.
            await SeedSessionAsync();

            foreach (var deadCode in new[] { 1001, 1003, 1004 })
            {
                var body = RecorderStartedBody.Replace("\"eventType\": 40", $"\"eventType\": {deadCode}");
                var result = await _service.ProcessAsync(Parse(body), CancellationToken.None);

                Assert.False(result.IsError); // ignored gracefully, never processed
            }

            var session = await _context.VideoSessions
                .FirstAsync(s => s.AgoraChannelName == Channel);

            Assert.Null(session.RecordingStatus);
            Assert.Null(session.RecordingStartedAtUtc);
        }

        [Fact]
        public async Task Recording_codes_are_ignored_when_productId_is_not_cloud_recording()
        {
            // eventType 4/11/31 collide with other products; routing must key on both.
            await SeedSessionAsync();

            var body = UploadedBody.Replace("\"productId\": 3", "\"productId\": 1");
            var result = await _service.ProcessAsync(Parse(body), CancellationToken.None);

            Assert.False(result.IsError);

            var session = await _context.VideoSessions
                .FirstAsync(s => s.AgoraChannelName == Channel);
            Assert.Null(session.RecordingCompletedAt);
        }
    }
}
