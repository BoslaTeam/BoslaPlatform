using BoslaPlatform.Application.Interfaces.Video;

namespace BoslaPlatform.Application.Features.VideoSessions.Interfaces;

public interface ITranscriptBroadcastService
{
    Task BroadcastTranscriptAsync(Guid sessionId, TranscriptSegment segment, CancellationToken ct = default);
}
