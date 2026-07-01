using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class ParticipantJoinedVideoSessionEventHandler
    : INotificationHandler<ParticipantJoinedVideoSessionEvent>
    {
        private readonly IVideoNotifier _notifier;

        public ParticipantJoinedVideoSessionEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            ParticipantJoinedVideoSessionEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.ParticipantJoinedAsync(
                notification.SessionId,
                notification.ParticipantId,
                cancellationToken);
        }
    }
}
