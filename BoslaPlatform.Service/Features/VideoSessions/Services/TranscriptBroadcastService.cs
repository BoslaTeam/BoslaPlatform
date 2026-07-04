using System.Diagnostics;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.VideoSessions.Services;

internal sealed class TranscriptBroadcastService : ITranscriptBroadcastService
{
    private readonly IVideoNotifier _videoNotifier;
    private readonly ILogger<TranscriptBroadcastService> _logger;

    public TranscriptBroadcastService(
        IVideoNotifier videoNotifier,
        ILogger<TranscriptBroadcastService> logger)
    {
        _videoNotifier = videoNotifier;
        _logger = logger;
    }

    public async Task BroadcastTranscriptAsync(Guid sessionId, TranscriptSegment segment, CancellationToken ct = default)
    {
        var targetGroup = sessionId.ToString();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Transcript broadcast started: SessionId={SessionId}, TargetGroup={TargetGroup}, SequenceNumber={Seq}, IsFinal={IsFinal}",
            sessionId, targetGroup, segment.SequenceNumber, segment.IsFinal);

        try
        {
            await _videoNotifier.TranscriptReceivedAsync(sessionId, segment, ct);

            sw.Stop();
            _logger.LogInformation(
                "Transcript broadcast completed: SessionId={SessionId}, TargetGroup={TargetGroup}, SequenceNumber={Seq}, IsFinal={IsFinal}, BroadcastDurationMs={DurationMs}",
                sessionId, targetGroup, segment.SequenceNumber, segment.IsFinal, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning(
                "Transcript broadcast cancelled: SessionId={SessionId}, TargetGroup={TargetGroup}, SequenceNumber={Seq}, BroadcastDurationMs={DurationMs}",
                sessionId, targetGroup, segment.SequenceNumber, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Transcript broadcast failed: SessionId={SessionId}, TargetGroup={TargetGroup}, SequenceNumber={Seq}, IsFinal={IsFinal}, BroadcastDurationMs={DurationMs}",
                sessionId, targetGroup, segment.SequenceNumber, segment.IsFinal, sw.ElapsedMilliseconds);
        }
    }
}
