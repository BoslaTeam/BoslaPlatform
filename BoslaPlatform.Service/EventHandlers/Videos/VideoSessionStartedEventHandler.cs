using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class VideoSessionStartedEventHandler
    : INotificationHandler<VideoSessionStartedEvent>
    {
        private readonly IVideoNotifier _notifier;

        public VideoSessionStartedEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            VideoSessionStartedEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.SessionStartedAsync(notification.SessionId, cancellationToken);
        }
    }
}
