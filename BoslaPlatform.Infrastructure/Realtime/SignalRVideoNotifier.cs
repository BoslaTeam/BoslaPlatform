using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Realtime
{
    public sealed class SignalRVideoNotifier : IVideoNotifier
    {
        private readonly IHubContext<VideoHub> _hubContext;
        private readonly ILogger<SignalRVideoNotifier> _logger;

        public SignalRVideoNotifier(
            IHubContext<VideoHub> hubContext,
            ILogger<SignalRVideoNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SessionStartedAsync(Guid sessionId, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying session started for {SessionId}", sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.SessionStarted, new { sessionId }, ct);
        }

        public async Task SessionEndedAsync(Guid sessionId, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying session ended for {SessionId}", sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.SessionEnded, new { sessionId }, ct);
        }

        public async Task ParticipantJoinedAsync(Guid sessionId, Guid participantId, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying participant {ParticipantId} joined session {SessionId}", participantId, sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.ParticipantJoined, new { sessionId, participantId }, ct);
        }

        public async Task ParticipantLeftAsync(Guid sessionId, Guid participantId, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying participant {ParticipantId} left session {SessionId}", participantId, sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.ParticipantLeft, new { sessionId, participantId }, ct);
        }

        public async Task RecordingStartedAsync(Guid sessionId, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying recording started for session {SessionId}", sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.RecordingStarted, new { sessionId }, ct);
        }

        public async Task RecordingCompletedAsync(Guid sessionId, string recordingUrl, CancellationToken ct = default)
        {
            _logger.LogInformation("Notifying recording completed for session {SessionId}", sessionId);
            await _hubContext.Clients
                .Group(sessionId.ToString())
                .SendAsync(VideoSignalREvents.RecordingStopped, new { sessionId, recordingUrl }, ct);
        }
    }
}
