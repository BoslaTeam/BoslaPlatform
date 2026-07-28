using BoslaPlatform.Domain.Models.Booking;
using Xunit;

namespace Bosla.Unit.Tests;

/// <summary>
/// Regression matrix for the appointment join/expiration time window
/// (<see cref="Appointment.CanJoinVideoSession"/>, <see cref="Appointment.CanStartVideoSession"/>).
///
/// This is the OTHER half of the "auto-expired before appointment started"
/// incident: STEP 3/4/5 of the investigation audited every time comparison in
/// this path and found all of them correctly UTC-based (DateTimeOffset end to
/// end, DB column type is `datetimeoffset`, no DateTime.Now / local-time usage
/// anywhere in the join or expiration paths). These tests pin that down as a
/// regression guard — the actual incident root cause was a domain state bug
/// (see VideoSessionChannelLifecycleTests), not a time-comparison bug, and these
/// tests prove the time comparisons hold correctly on their own.
///
/// Business rule under test: join opens 10 minutes before Start and closes at End.
/// </summary>
public class AppointmentJoinWindowTests
{
    private static Appointment Schedule(DateTimeOffset start, DateTimeOffset end) =>
        Appointment.Schedule(
            specialistId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            start: start,
            end: end,
            sessionTopic: null,
            notes: null,
            sessionPrice: 100m);

    [Fact]
    public void Appointment_tomorrow_cannot_be_joined_yet()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddDays(1), now.AddDays(1).AddHours(1));

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.JoinTooEarly", result.Errors[0].Code);
    }

    [Fact]
    public void Appointment_in_one_hour_cannot_be_joined_yet()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddHours(1), now.AddHours(2));

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.JoinTooEarly", result.Errors[0].Code);
    }

    [Fact]
    public void Appointment_in_fifteen_minutes_cannot_be_joined_yet()
    {
        // Outside the 10-minute join window — must still be rejected as too early.
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddMinutes(15), now.AddMinutes(75));

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.JoinTooEarly", result.Errors[0].Code);
    }

    [Fact]
    public void Appointment_in_nine_minutes_can_be_joined()
    {
        // Inside the 10-minute join window — must be allowed.
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddMinutes(9), now.AddMinutes(69));

        var result = appointment.CanJoinVideoSession(now);

        Assert.False(result.IsError);
    }

    [Fact]
    public void Appointment_currently_running_can_be_joined()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddMinutes(-10), now.AddMinutes(50));

        var result = appointment.CanJoinVideoSession(now);

        Assert.False(result.IsError);
    }

    [Fact]
    public void Appointment_finished_cannot_be_joined()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddHours(-2), now.AddMinutes(-1));

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.SessionExpired", result.Errors[0].Code);
    }

    [Fact]
    public void Appointment_long_expired_cannot_be_joined()
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddDays(-1), now.AddDays(-1).AddHours(1));

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.SessionExpired", result.Errors[0].Code);
    }

    [Fact]
    public void Appointment_exactly_at_end_boundary_is_expired()
    {
        // currentTime >= End is the documented boundary condition.
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddMinutes(-30), now);

        var result = appointment.CanJoinVideoSession(now);

        Assert.True(result.IsError);
        Assert.Equal("VideoSession.SessionExpired", result.Errors[0].Code);
    }

    [Fact]
    public void StartVideoSession_window_is_independent_of_join_window()
    {
        // CanStartVideoSession is scoped to +/-15 minutes around Start, distinct
        // from the join window (Start-10min to End). Verified here so the two
        // are never conflated by a future change.
        var now = DateTimeOffset.UtcNow;
        var appointment = Schedule(now.AddMinutes(20), now.AddMinutes(80));

        var startResult = appointment.CanStartVideoSession(now);

        Assert.True(startResult.IsError);
        Assert.Equal("Appointment.TooEarly", startResult.Errors[0].Code);
    }
}
