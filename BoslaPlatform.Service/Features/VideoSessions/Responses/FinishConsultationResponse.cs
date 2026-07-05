namespace BoslaPlatform.Application.Features.VideoSessions.Responses
{
    public sealed record FinishConsultationResponse(
    Guid VideoSessionId,
    DateTime CompletedAt);
}
