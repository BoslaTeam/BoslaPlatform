namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record StartVideoSessionResponse(
    Guid VideoSessionId,
    DateTime StartedAt);
}
