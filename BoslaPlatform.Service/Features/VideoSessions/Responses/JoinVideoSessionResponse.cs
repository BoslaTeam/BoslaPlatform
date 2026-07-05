namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record JoinVideoSessionResponse(
    Guid VideoSessionId,
    Guid UserId,
    DateTime JoinedAt);
}
