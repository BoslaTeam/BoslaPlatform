using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class VideoSessionStartedEventHandler
    : INotificationHandler<VideoSessionStartedEvent>
    {
        public Task Handle(
            VideoSessionStartedEvent notification,
            CancellationToken cancellationToken)
        {
            // Future:
            // Send notification
            // Analytics
            // Recording

            return Task.CompletedTask;
        }
    }
}
