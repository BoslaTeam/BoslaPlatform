namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record EndVideoSessionResponse(
    Guid VideoSessionId,
    DateTime EndedAt);
}
