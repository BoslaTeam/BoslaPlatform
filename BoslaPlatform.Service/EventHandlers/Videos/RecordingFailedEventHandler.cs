using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class RecordingFailedEventHandler
    : INotificationHandler<RecordingFailedEvent>
    {
        private readonly IVideoNotifier _notifier;

        public RecordingFailedEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            RecordingFailedEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.RecordingCompletedAsync(
                notification.SessionId,
                string.Empty,
                cancellationToken);
        }
    }
}
