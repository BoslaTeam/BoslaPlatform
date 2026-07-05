namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record LeaveVideoSessionResponse(
    Guid VideoSessionId,
    Guid UserId,
    DateTime LeftAt);
}
