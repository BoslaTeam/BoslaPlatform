namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public sealed record JoinVideoSessionResponse(
    Guid VideoSessionId,
    Guid UserId,
    DateTime JoinedAt);
}
