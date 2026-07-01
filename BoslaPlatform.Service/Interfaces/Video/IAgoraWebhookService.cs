using BoslaPlatform.Application.Features.VideoSessions.Requests;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface IAgoraWebhookService
    {
        Task<Result<bool>> HandleAsync(
            AgoraWebhookEvent webhookEvent,
            CancellationToken ct);
    }
}
