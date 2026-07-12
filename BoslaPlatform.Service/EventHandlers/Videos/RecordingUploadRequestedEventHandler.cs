using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class RecordingUploadRequestedEventHandler
    : INotificationHandler<RecordingUploadRequestedEvent>
    {
        private readonly IVideoNotifier _notifier;
        private readonly ILogger<RecordingUploadRequestedEventHandler> _logger;

        public RecordingUploadRequestedEventHandler(
            IVideoNotifier notifier,
            ILogger<RecordingUploadRequestedEventHandler> logger)
        {
            _notifier = notifier;
            _logger = logger;
        }

        public async Task Handle(
            RecordingUploadRequestedEvent notification,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Recording upload requested for session {SessionId}, resourceId={ResourceId}, sid={Sid}",
                notification.SessionId,
                notification.ResourceId,
                notification.Sid);

            await _notifier.RecordingCompletedAsync(
                notification.SessionId,
                string.Empty,
                cancellationToken);
        }
    }
}