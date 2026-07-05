namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record StartRecordingResponse(
        Guid SessionId,
        Guid RecordingId,
        DateTime StartedAtUtc);
}
