using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.VideoSessions.Services
{
    public class VideoSessionLifecycleService : IVideoSessionLifecycleService
    {
        private readonly IAppDbContext _context;
        private readonly IRecordingCompletionService _completionService;
        private readonly ISTTProvider _sttProvider;
        private readonly ILogger<VideoSessionLifecycleService> _logger;

        public VideoSessionLifecycleService(
            IAppDbContext context,
            IRecordingCompletionService completionService,
            ISTTProvider sttProvider,
            ILogger<VideoSessionLifecycleService> logger)
        {
            _context = context;
            _completionService = completionService;
            _sttProvider = sttProvider;
            _logger = logger;
        }

        public async Task<Result> CompleteSessionAsync(
            Guid sessionId,
            VideoSessionCompletionReason reason,
            CancellationToken ct = default)
        {
            var session = await _context.VideoSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId, ct);

            if (session is null)
                return Error.NotFound("VideoSession.NotFound", "Video session was not found.");

            _logger.LogInformation(
                "CompleteSessionAsync start | SessionId={SessionId} | Status={Status} | Reason={Reason} | UtcNow={UtcNow}",
                session.Id, session.Status, reason,
                DateTimeOffset.UtcNow.ToString("O"));

            // Domain now allows Ended -> Completed (the ChannelDestroyed webhook may
            // have already moved the session to Ended). Both the manual finish and the
            // automatic expiration paths converge on Complete() for an identical final
            // state — so a genuine failure here is a real error, not a race workaround.
            var completeResult = session.Complete();
            if (completeResult.IsError)
            {
                _logger.LogWarning(
                    "Failed to complete session {SessionId}: {ErrorCode} - {ErrorMessage}",
                    session.Id, completeResult.Errors[0].Code, completeResult.Errors[0].Description);
                return completeResult;
            }

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Manual finish (specialist) and automatic expiration can collide on the
                // same row near the appointment's natural end. The losing writer must not
                // surface a 500 — treat an already-terminal session as an idempotent
                // success. The winning writer performs recording/STT cleanup.
                var current = await _context.VideoSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sessionId, ct);

                if (current is not null
                    && current.Status is VideoSessionStatus.Completed or VideoSessionStatus.Ended)
                {
                    _logger.LogInformation(
                        "CompleteSessionAsync | SessionId={SessionId} already terminal ({Status}) after " +
                        "concurrent completion; treating as idempotent success. Reason={Reason}",
                        sessionId, current.Status, reason);
                    return Result.Success();
                }

                throw;
            }

            _logger.LogInformation(
                "Session {SessionId} persisted as {Status} (reason: {Reason})",
                session.Id, session.Status, reason);

            // --------------------------------------------------------------
            // Best-effort cleanup: Recording + STT
            // The session is already persisted as Completed above. Nothing in this
            // block may prevent completion — every failure, whether returned as a
            // Result error OR thrown as an exception by the provider stack, is
            // swallowed and logged so the caller always sees success.
            // --------------------------------------------------------------
            try
            {
                // Finish the recording through the SINGLE completion pipeline so an
                // auto-completed session (expiration / channel destroyed / End) runs
                // the identical Stop → verify upload → HeadObject → persist flow as a
                // manual Stop. The service is idempotent, so a recording already
                // finalized via the command path is a no-op here (no redundant stop).
                if (!string.IsNullOrEmpty(session.AgoraRecordingId)
                    && session.RecordingStatus != RecordingStatus.Completed)
                {
                    _logger.LogInformation(
                        "Completing recording for session {SessionId} via shared pipeline (reason {Reason}).",
                        session.Id, reason);

                    var completion = await _completionService.CompleteAsync(
                        session.Id, RecordingCompletionTrigger.SessionEnded, hint: null, ct);

                    if (completion.IsError)
                    {
                        _logger.LogWarning(
                            "Best-effort recording completion failed for session {SessionId}: {ErrorCode} - {ErrorMessage}",
                            session.Id, completion.Errors[0].Code, completion.Errors[0].Description);
                    }
                }

                var sttResult = await _sttProvider.StopSTTAsync(
                    session.AgoraChannelName, ct);

                if (sttResult.IsError)
                {
                    _logger.LogWarning(
                        "Best-effort STT stop failed for session {SessionId}: {ErrorCode} - {ErrorMessage}",
                        session.Id, sttResult.Errors[0].Code, sttResult.Errors[0].Description);
                }
            }
            catch (Exception ex)
            {
                // Never let a best-effort cleanup failure turn a completed session into
                // a failed API call. The session state is already persisted.
                _logger.LogWarning(ex,
                    "Best-effort recording/STT cleanup threw for session {SessionId}; " +
                    "session remains Completed. Reason={Reason}",
                    session.Id, reason);
            }

            _logger.LogInformation(
                "CompleteSessionAsync done | SessionId={SessionId} | Status={Status}",
                session.Id, session.Status);

            return Result.Success();
        }
    }
}
