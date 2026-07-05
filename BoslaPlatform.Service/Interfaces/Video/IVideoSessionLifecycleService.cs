using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface IVideoSessionLifecycleService
    {
        Task<Result> CompleteSessionAsync(
            Guid sessionId,
            VideoSessionCompletionReason reason,
            CancellationToken ct = default);
    }
}
