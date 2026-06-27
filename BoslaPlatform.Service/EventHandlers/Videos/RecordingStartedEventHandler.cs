using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class RecordingStartedEventHandler
    : INotificationHandler<RecordingStartedEvent>
    {
        private readonly IVideoNotifier _notifier;

        public RecordingStartedEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            RecordingStartedEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.RecordingStartedAsync(notification.SessionId, cancellationToken);
        }
    }
}
