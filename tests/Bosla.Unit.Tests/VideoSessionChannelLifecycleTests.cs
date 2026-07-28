using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using Xunit;

namespace Bosla.Unit.Tests;

/// <summary>
/// Regression coverage for the "session auto-expired before the appointment even
/// started" incident.
///
/// ROOT CAUSE: <see cref="VideoSession.ChannelDestroyed"/> transitioned the
/// aggregate to <see cref="VideoSessionStatus.Ended"/> from ANY non-Ended status,
/// including <see cref="VideoSessionStatus.Waiting"/> — the state a session sits
/// in before anyone has genuinely joined (ChannelCreated never fired). Agora's
/// channel_destroy webhook (eventType 111) reflects the emptiness of the Agora
/// RTC channel, which is infrastructure state independent of our own business
/// state: a client preview/connectivity-check connection, a stale or duplicate
/// webhook delivery, or out-of-order delivery relative to channel_create can all
/// cause it to fire while the session is still Waiting — including for an
/// appointment whose scheduled window hasn't opened yet.
///
/// Once Status flipped to Ended, VideoSessionService.JoinAsync's FIRST check
/// (before the appointment time-window check) rejects the join with
/// "VideoSession.Ended" — immediately, and independent of whether the real
/// appointment start time has arrived. That produced the reported symptom.
///
/// FIX: ChannelDestroyed now only transitions Active -> Ended (a genuine end of
/// a session someone actually joined). Waiting -> Ended, Ended -> Ended, and
/// Completed -> Ended are all idempotent no-ops — mirroring the existing
/// symmetric guard already on ChannelCreated (Waiting -> Active only).
/// </summary>
public class VideoSessionChannelLifecycleTests
{
    private static VideoSession CreateWaitingSession() =>
        VideoSession.Create(Guid.NewGuid(), "chan-1", "app-1", VideoSessionType.OneToOne).Value;

    [Fact]
    public void ChannelDestroyed_while_Waiting_is_a_no_op_session_stays_joinable()
    {
        // This is the exact incident scenario: nobody has joined yet (no
        // ChannelCreated), but Agora sends channel_destroy anyway.
        var session = CreateWaitingSession();

        var result = session.ChannelDestroyed("chan-1", DateTimeOffset.UtcNow);

        Assert.False(result.IsError);
        Assert.Equal(VideoSessionStatus.Waiting, session.Status);
        Assert.Null(session.EndedAt);

        // The session must still be genuinely joinable afterwards — this is the
        // regression assertion: before the fix, Status would be Ended here and
        // VideoSessionService.JoinAsync would permanently reject any future join.
        Assert.True(session.Status is not (VideoSessionStatus.Ended or VideoSessionStatus.Completed));
    }

    [Fact]
    public void ChannelDestroyed_while_Active_ends_the_session()
    {
        // A real participant joined first (ChannelCreated -> Active); this is the
        // genuine "last participant left" signal and must still end the session.
        var session = CreateWaitingSession();
        session.ChannelCreated("chan-1", DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.Equal(VideoSessionStatus.Active, session.Status);

        var occurredAt = DateTimeOffset.UtcNow;
        var result = session.ChannelDestroyed("chan-1", occurredAt);

        Assert.False(result.IsError);
        Assert.Equal(VideoSessionStatus.Ended, session.Status);
        Assert.Equal(occurredAt.UtcDateTime, session.EndedAt);
    }

    [Fact]
    public void ChannelDestroyed_is_idempotent_when_already_Ended()
    {
        var session = CreateWaitingSession();
        session.ChannelCreated("chan-1", DateTimeOffset.UtcNow.AddMinutes(-5));
        var firstOccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        session.ChannelDestroyed("chan-1", firstOccurredAt);

        // A duplicate/retried webhook delivery arrives later.
        var duplicateResult = session.ChannelDestroyed("chan-1", DateTimeOffset.UtcNow);

        Assert.False(duplicateResult.IsError);
        Assert.Equal(VideoSessionStatus.Ended, session.Status);
        // EndedAt must not be overwritten by the duplicate delivery.
        Assert.Equal(firstOccurredAt.UtcDateTime, session.EndedAt);
    }

    [Fact]
    public void ChannelDestroyed_while_Completed_is_a_no_op()
    {
        var session = CreateWaitingSession();
        session.ChannelCreated("chan-1", DateTimeOffset.UtcNow.AddMinutes(-30));
        session.Complete();
        Assert.Equal(VideoSessionStatus.Completed, session.Status);

        var result = session.ChannelDestroyed("chan-1", DateTimeOffset.UtcNow);

        Assert.False(result.IsError);
        Assert.Equal(VideoSessionStatus.Completed, session.Status);
    }

    [Fact]
    public void ChannelCreated_after_a_phantom_ChannelDestroyed_still_activates_normally()
    {
        // End-to-end regression: phantom destroy while Waiting must not poison the
        // session's ability to later activate for real when the participant
        // genuinely joins within the appointment window.
        var session = CreateWaitingSession();

        session.ChannelDestroyed("chan-1", DateTimeOffset.UtcNow.AddMinutes(-9));
        var result = session.ChannelCreated("chan-1", DateTimeOffset.UtcNow);

        Assert.False(result.IsError);
        Assert.Equal(VideoSessionStatus.Active, session.Status);
    }
}
