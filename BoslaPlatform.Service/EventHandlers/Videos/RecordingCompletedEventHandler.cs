using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class RecordingCompletedEventHandler
    : INotificationHandler<RecordingCompletedEvent>
    {
        private readonly IVideoNotifier _notifier;

        public RecordingCompletedEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            RecordingCompletedEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.RecordingCompletedAsync(
                notification.SessionId,
                notification.RecordingUrl,
                cancellationToken);
        }
    }
}
