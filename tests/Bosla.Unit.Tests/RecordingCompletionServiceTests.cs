using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

/// <summary>
/// Proves the unification mandate: manual Stop, automatic expiration (SessionEnded)
/// and the Agora webhooks all run the SAME completion pipeline and land on the
/// IDENTICAL final state — verified Completed + S3 metadata.
/// </summary>
public class RecordingCompletionServiceTests
{
    private const string Bucket = "bosla-recordings-test";
    private const string Channel = "chan-unify";
    private const string ObjectKey = "recordings/sid-1_chan-unify.m3u8";

    private sealed record Harness(AppDbContext Context, RecordingCompletionService Service, Guid SessionId);

    private static Harness Build(bool providerStopReturnsFile)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Completion_{Guid.NewGuid()}")
            .Options);

        var session = VideoSession.Create(Guid.NewGuid(), Channel, "app", VideoSessionType.OneToOne).Value;
        session.ConfirmRecordingStarted("resource-1", "sid-1"); // → Recording + identifiers
        session.SetRecording("resource-1", "sid-1", "uid-1");
        context.VideoSessions.Add(session);
        context.SaveChanges();

        var provider = new Mock<IRecordingProvider>();
        provider
            .Setup(p => p.StopRecordingAsync(Channel, "resource-1", "sid-1", "uid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StopRecordingResult>.Success(new StopRecordingResult(
                providerStopReturnsFile ? ObjectKey : string.Empty,
                DurationSeconds: 42,
                FileSizeBytes: 0,
                UploadingStatus: AgoraUploadingStatus.Uploaded)));

        var storage = new Mock<IRecordingStorage>();
        storage
            .Setup(s => s.HeadObjectAsync(Bucket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RecordingObjectMetadata>.Success(
                new RecordingObjectMetadata(1234, "application/vnd.apple.mpegurl", "\"etag\"", DateTime.UtcNow, "STANDARD")));

        var settings = new Mock<IRecordingStorageSettings>();
        settings.SetupGet(s => s.RecordingBucketName).Returns(Bucket);

        var verifier = new RecordingUploadVerifier(
            provider.Object, storage.Object,
            Options.Create(new RecordingUploadVerificationOptions { MaxAttempts = 1, BaseDelaySeconds = 1 }),
            Mock.Of<ILogger<RecordingUploadVerifier>>());

        var service = new RecordingCompletionService(
            context, provider.Object, verifier, settings.Object,
            Mock.Of<IRecordingPipelineLog>(), Mock.Of<ILogger<RecordingCompletionService>>());

        return new Harness(context, service, session.Id);
    }

    private static void AssertVerifiedCompleted(VideoSession session)
    {
        Assert.Equal(RecordingStatus.Completed, session.RecordingStatus);
        Assert.NotNull(session.RecordingCompletedAt);
        Assert.Equal(ObjectKey, session.ObjectKey);
        Assert.Equal(Bucket, session.BucketName);
        Assert.Equal(1234, session.ContentLength);
        Assert.Equal(42, session.DurationSeconds);
        Assert.True(session.IsUploadedToS3);
        Assert.Equal(StorageProvider.AmazonS3, session.StorageProvider);
    }

    [Theory]
    [InlineData(RecordingCompletionTrigger.ManualStop)]
    [InlineData(RecordingCompletionTrigger.SessionEnded)]
    public async Task StopInitiated_triggers_reach_verified_completed(RecordingCompletionTrigger trigger)
    {
        var h = Build(providerStopReturnsFile: true);

        var result = await h.Service.CompleteAsync(h.SessionId, trigger);

        Assert.False(result.IsError);
        Assert.Equal(UploadVerificationOutcome.Verified, result.Value.VerificationOutcome);

        var session = await h.Context.VideoSessions.FirstAsync(s => s.Id == h.SessionId);
        AssertVerifiedCompleted(session);
    }

    [Fact]
    public async Task WebhookConfirmed_reaches_identical_verified_completed_without_stopping()
    {
        var h = Build(providerStopReturnsFile: true);

        var result = await h.Service.CompleteAsync(
            h.SessionId,
            RecordingCompletionTrigger.WebhookConfirmed,
            new RecordingProviderHint(ObjectKey, AgoraUploadingStatus.Uploaded, DurationSeconds: 42));

        Assert.False(result.IsError);
        Assert.Equal(UploadVerificationOutcome.Verified, result.Value.VerificationOutcome);

        var session = await h.Context.VideoSessions.FirstAsync(s => s.Id == h.SessionId);
        AssertVerifiedCompleted(session);
    }

    [Fact]
    public async Task Completion_is_idempotent_when_already_completed()
    {
        var h = Build(providerStopReturnsFile: true);

        // First completion → Completed.
        await h.Service.CompleteAsync(h.SessionId, RecordingCompletionTrigger.ManualStop);

        // A duplicate (e.g. a late webhook) must be a safe no-op, not a re-completion.
        var second = await h.Service.CompleteAsync(
            h.SessionId, RecordingCompletionTrigger.WebhookConfirmed,
            new RecordingProviderHint(ObjectKey, AgoraUploadingStatus.Uploaded, DurationSeconds: 42));

        Assert.False(second.IsError);
        Assert.Equal(RecordingStatus.Completed, second.Value.FinalStatus);

        var session = await h.Context.VideoSessions.FirstAsync(s => s.Id == h.SessionId);
        AssertVerifiedCompleted(session);
    }
}
