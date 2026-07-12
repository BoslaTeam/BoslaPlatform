using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Video;
using static BoslaPlatform.Domain.Enums.StorageProvider;
using Xunit;

namespace Bosla.Unit.Tests;

public class VideoRecordingWorkflowTests
{
    private static VideoSession CreateActiveSession()
    {
        var sessionResult = VideoSession.Create(
            Guid.NewGuid(),
            "channel-test",
            "app-id",
            VideoSessionType.OneToOne);
        var session = sessionResult.Value;
        session.Start();
        return session;
    }

    private static ScreenRecording CreateRecording(VideoSession session)
    {
        var recordingResult = ScreenRecording.Create(
            session.Id,
            RecordingAccessControl.Both,
            RecordingStorageProvider.Agora);
        return recordingResult.Value;
    }

    [Fact]
    public void StartRecording_twice_returns_conflict()
    {
        var session = CreateActiveSession();
        var recording1 = CreateRecording(session);
        session.StartRecording(recording1);
        session.SetCurrentRecording(recording1);

        var recording2 = CreateRecording(session);
        var result = session.StartRecording(recording2);

        Assert.True(result.IsError);
        Assert.Contains("AlreadyRecording", result.Errors[0].Code);
    }

    [Fact]
    public void StopRecording_before_start_returns_validation_error()
    {
        var session = CreateActiveSession();

        var result = session.StopRecording();

        Assert.True(result.IsError);
        Assert.Contains("NotRecording", result.Errors[0].Code);
    }

    [Fact]
    public void StopRecording_after_stop_returns_validation_error()
    {
        var session = CreateActiveSession();
        var recording = CreateRecording(session);
        session.StartRecording(recording);
        session.SetCurrentRecording(recording);

        session.StopRecording();

        var result = session.StopRecording();
        Assert.True(result.IsError);
        Assert.Contains("NotRecording", result.Errors[0].Code);
    }

    [Fact]
    public void FailActiveRecording_sets_failure_reason()
    {
        var session = CreateActiveSession();
        var recording = CreateRecording(session);
        session.StartRecording(recording);
        session.SetCurrentRecording(recording);

        session.FailActiveRecording("Test failure reason");

        Assert.Equal(RecordingStatus.Failed, session.RecordingStatus);
        Assert.Equal("Test failure reason", session.RecordingFailureReason);
    }

    [Fact]
    public void FailActiveRecording_without_reason_sets_null()
    {
        var session = CreateActiveSession();
        var recording = CreateRecording(session);
        session.StartRecording(recording);
        session.SetCurrentRecording(recording);

        session.FailActiveRecording();

        Assert.Equal(RecordingStatus.Failed, session.RecordingStatus);
        Assert.Null(session.RecordingFailureReason);
    }

    [Fact]
    public void StartRecording_fails_when_session_not_active()
    {
        var sessionResult = VideoSession.Create(
            Guid.NewGuid(),
            "channel-test",
            "app-id",
            VideoSessionType.OneToOne);
        var session = sessionResult.Value;
        var recording = CreateRecording(session);

        var result = session.StartRecording(recording);

        Assert.True(result.IsError);
        Assert.Contains("NotActive", result.Errors[0].Code);
    }

    [Fact]
    public void FailActiveRecording_when_not_recording_returns_error()
    {
        var session = CreateActiveSession();

        var result = session.FailActiveRecording("reason");

        Assert.True(result.IsError);
        Assert.Contains("NotRecording", result.Errors[0].Code);
    }

    [Fact]
    public void RecordingStatus_enum_contains_new_values()
    {
        Assert.Contains(RecordingStatus.Idle, Enum.GetValues<RecordingStatus>());
        Assert.Contains(RecordingStatus.Uploading, Enum.GetValues<RecordingStatus>());
        Assert.Contains(RecordingStatus.Uploaded, Enum.GetValues<RecordingStatus>());
        Assert.Contains(RecordingStatus.Cancelled, Enum.GetValues<RecordingStatus>());
    }

    [Fact]
    public void RecordingStatus_enum_values_are_distinct()
    {
        var values = Enum.GetValues<RecordingStatus>()
            .Cast<int>()
            .ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void StopRecording_raises_upload_requested_event()
    {
        var session = CreateActiveSession();
        var recording = CreateRecording(session);
        session.StartRecording(recording);
        session.SetCurrentRecording(recording);

        session.StopRecording();

        var hasUploadEvent = session.DomainEvents
            .Any(e => e is RecordingUploadRequestedEvent);

        Assert.True(hasUploadEvent);
    }

    // ──────────────────────────────────────────────
    // Upload lifecycle methods
    // ──────────────────────────────────────────────

    [Fact]
    public void MarkUploadPending_sets_status_and_increments_attempts()
    {
        var session = CreateActiveSession();

        session.MarkUploadPending();

        Assert.Equal(UploadStatus.Pending, session.UploadStatus);
        Assert.Equal(1, session.UploadAttempts);
    }

    [Fact]
    public void MarkUploading_sets_status()
    {
        var session = CreateActiveSession();

        session.MarkUploading();

        Assert.Equal(UploadStatus.Uploading, session.UploadStatus);
    }

    [Fact]
    public void MarkUploadSucceeded_sets_all_fields()
    {
        var session = CreateActiveSession();

        session.MarkUploadSucceeded(
            CloudflareR2,
            "recordings",
            "session-1/file.mp4",
            "video/mp4",
            1024L);

        Assert.Equal(UploadStatus.Uploaded, session.UploadStatus);
        Assert.Equal(CloudflareR2, session.StorageProvider);
        Assert.Equal("recordings", session.BucketName);
        Assert.Equal("session-1/file.mp4", session.ObjectKey);
        Assert.Equal("video/mp4", session.ContentType);
        Assert.Equal(1024L, session.ContentLength);
        Assert.NotNull(session.UploadedAtUtc);
        Assert.Null(session.LastUploadError);
    }

    [Fact]
    public void MarkUploadFailed_sets_status_and_error()
    {
        var session = CreateActiveSession();

        session.MarkUploadFailed("Connection timeout");

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Equal("Connection timeout", session.LastUploadError);
    }

    [Fact]
    public void MarkUploadRetrying_sets_retrying_status()
    {
        var session = CreateActiveSession();

        session.MarkUploadRetrying();

        Assert.Equal(UploadStatus.Retrying, session.UploadStatus);
    }

    [Fact]
    public void MarkUploadCancelled_sets_cancelled_status()
    {
        var session = CreateActiveSession();

        session.MarkUploadCancelled();

        Assert.Equal(UploadStatus.Cancelled, session.UploadStatus);
    }

    [Fact]
    public void Upload_status_cycle_pending_to_uploading_to_uploaded()
    {
        var session = CreateActiveSession();

        session.MarkUploadPending();
        Assert.Equal(UploadStatus.Pending, session.UploadStatus);

        session.MarkUploading();
        Assert.Equal(UploadStatus.Uploading, session.UploadStatus);

        session.MarkUploadSucceeded(CloudflareR2, "bucket", "key", "text/plain", 42);
        Assert.Equal(UploadStatus.Uploaded, session.UploadStatus);
    }

    [Fact]
    public void Upload_status_cycle_pending_to_uploading_to_failed()
    {
        var session = CreateActiveSession();

        session.MarkUploadPending();
        session.MarkUploading();
        session.MarkUploadFailed("Disk full");

        Assert.Equal(UploadStatus.Failed, session.UploadStatus);
        Assert.Equal("Disk full", session.LastUploadError);
    }

    [Fact]
    public void IsUploadPendingOrRetrying_true_when_pending()
    {
        var session = CreateActiveSession();
        session.MarkUploadPending();

        Assert.True(session.IsUploadPendingOrRetrying);
    }

    [Fact]
    public void IsUploadPendingOrRetrying_true_when_retrying()
    {
        var session = CreateActiveSession();
        session.MarkUploadRetrying();

        Assert.True(session.IsUploadPendingOrRetrying);
    }

    [Fact]
    public void IsUploadPendingOrRetrying_false_when_uploaded()
    {
        var session = CreateActiveSession();
        session.MarkUploadSucceeded(CloudflareR2, "bucket", "key", "text/plain", 0);

        Assert.False(session.IsUploadPendingOrRetrying);
    }

    [Fact]
    public void UploadAttempts_increment_on_each_pending_mark()
    {
        var session = CreateActiveSession();

        session.MarkUploadPending();
        session.MarkUploadPending();
        session.MarkUploadPending();

        Assert.Equal(3, session.UploadAttempts);
    }

    [Fact]
    public void UploadedAtUtc_only_set_after_success()
    {
        var session = CreateActiveSession();
        Assert.Null(session.UploadedAtUtc);

        session.MarkUploadPending();
        Assert.Null(session.UploadedAtUtc);

        session.MarkUploadSucceeded(CloudflareR2, "b", "k", "t", 1);
        Assert.NotNull(session.UploadedAtUtc);
    }
}
