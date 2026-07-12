using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Video;
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
}
