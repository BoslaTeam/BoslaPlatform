using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class VideoSessionEndedRealtimeHandler
    : INotificationHandler<VideoSessionEndedEvent>
    {
        private readonly IVideoNotifier _notifier;

        public VideoSessionEndedRealtimeHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            VideoSessionEndedEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.SessionEndedAsync(notification.SessionId, notification.EndedAtUtc, cancellationToken);
        }
    }
}
