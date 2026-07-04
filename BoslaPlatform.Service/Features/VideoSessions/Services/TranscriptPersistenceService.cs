using System.Diagnostics;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranscriptEntity = BoslaPlatform.Domain.Models.Video.TranscriptSegment;

namespace BoslaPlatform.Application.Features.VideoSessions.Services;

internal sealed class TranscriptPersistenceService : ITranscriptPersistenceService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<TranscriptPersistenceService> _logger;

    public TranscriptPersistenceService(
        IAppDbContext context,
        ILogger<TranscriptPersistenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> PersistFinalTranscriptAsync(Guid sessionId, TranscriptSegment segment, CancellationToken ct = default)
    {
        if (!segment.IsFinal)
        {
            return Result.Success();
        }

        var exists = await _context.TranscriptSegments
            .AnyAsync(x => x.VideoSessionId == sessionId && x.SequenceNumber == segment.SequenceNumber, ct);

        if (exists)
        {
            _logger.LogWarning(
                "Duplicate transcript skipped: SessionId={SessionId}, SequenceNumber={Seq}",
                sessionId, segment.SequenceNumber);
            return Result.Success();
        }

        var sw = Stopwatch.StartNew();

        var session = await _context.VideoSessions.FindAsync(new object[] { sessionId }, ct);

        if (session is null)
        {
            return Error.NotFound(
                "VideoSession.NotFound",
                "Video session was not found.");
        }

        var createResult = TranscriptEntity.Create(
            sessionId,
            segment.SequenceNumber,
            segment.Text,
            segment.Language,
            segment.SpeakerId,
            segment.SpeakerLabel,
            segment.TimestampUtc,
            segment.Offset);

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        var addResult = session.AddTranscriptSegment(entity);

        if (addResult.IsError)
        {
            return addResult.Errors;
        }

        _context.TranscriptSegments.Add(entity);
        await _context.SaveChangesAsync(ct);

        sw.Stop();
        _logger.LogInformation(
            "Transcript persisted: SessionId={SessionId}, SequenceNumber={Seq}, DurationMs={DurationMs}",
            sessionId, segment.SequenceNumber, sw.ElapsedMilliseconds);

        return Result.Success();
    }
}
