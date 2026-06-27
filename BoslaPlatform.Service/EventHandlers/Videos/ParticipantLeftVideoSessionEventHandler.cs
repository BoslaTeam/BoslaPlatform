using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class ParticipantLeftVideoSessionEventHandler
    : INotificationHandler<ParticipantLeftVideoSessionEvent>
    {
        private readonly IVideoNotifier _notifier;

        public ParticipantLeftVideoSessionEventHandler(IVideoNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(
            ParticipantLeftVideoSessionEvent notification,
            CancellationToken cancellationToken)
        {
            await _notifier.ParticipantLeftAsync(
                notification.SessionId,
                notification.ParticipantId,
                cancellationToken);
        }
    }
}
