using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.VideoSessions.Interfaces;

public interface ITranscriptPersistenceService
{
    Task<Result> PersistFinalTranscriptAsync(Guid sessionId, TranscriptSegment segment, CancellationToken ct = default);
}
