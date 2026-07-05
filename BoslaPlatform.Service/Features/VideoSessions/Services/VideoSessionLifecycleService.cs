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
        private readonly IRecordingProvider _recordingProvider;
        private readonly ISTTProvider _sttProvider;
        private readonly ILogger<VideoSessionLifecycleService> _logger;

        public VideoSessionLifecycleService(
            IAppDbContext context,
            IRecordingProvider recordingProvider,
            ISTTProvider sttProvider,
            ILogger<VideoSessionLifecycleService> logger)
        {
            _context = context;
            _recordingProvider = recordingProvider;
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
                "Completing session {SessionId} with reason {Reason}",
                session.Id, reason);

            bool hadActiveRecording = session.IsRecording;

            var completeResult = session.Complete();
            if (completeResult.IsError)
            {
                _logger.LogWarning(
                    "Failed to complete session {SessionId}: {ErrorCode} - {ErrorMessage}",
                    session.Id, completeResult.Errors[0].Code, completeResult.Errors[0].Description);
                return completeResult;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Session {SessionId} completed successfully (reason: {Reason})",
                session.Id, reason);

            // TODO: Emit session completion metrics
            //   - CompletedSessions counter (tagged by reason)
            //   - ExpiredSessions counter (when reason == AppointmentExpired)
            //   - AverageCompletionDuration histogram
            //   - RecordingStopFailureCount (when recording stop fails)
            //   - SttStopFailureCount (when STT stop fails)

            if (hadActiveRecording && !string.IsNullOrEmpty(session.AgoraRecordingId))
            {
                _logger.LogInformation(
                    "Stopping recording for completed session {SessionId}", session.Id);

                var stopResult = await _recordingProvider.StopRecordingAsync(
                    session.AgoraChannelName,
                    session.AgoraRecordingId,
                    session.AgoraRecordingSid,
                    ct);

                if (stopResult.IsError)
                {
                    _logger.LogWarning(
                        "Failed to stop recording for session {SessionId}: {ErrorCode} - {ErrorMessage}",
                        session.Id, stopResult.Errors[0].Code, stopResult.Errors[0].Description);
                }
            }

            _logger.LogInformation(
                "Stopping STT for completed session {SessionId}", session.Id);

            var sttResult = await _sttProvider.StopSTTAsync(
                session.AgoraChannelName, ct);

            if (sttResult.IsError)
            {
                _logger.LogWarning(
                    "Failed to stop STT for session {SessionId}: {ErrorCode} - {ErrorMessage}",
                    session.Id, sttResult.Errors[0].Code, sttResult.Errors[0].Description);
            }

            return Result.Success();
        }
    }
}
