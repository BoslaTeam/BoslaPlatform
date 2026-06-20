using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Domain.Events.Conversations;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Communications
{
    public sealed class MessageDeletedEventHandler
    : INotificationHandler<MessageDeletedEvent>
    {
        private readonly IChatNotifier _chatNotifier;
        public MessageDeletedEventHandler(IChatNotifier chatNotifier)
        {
            _chatNotifier = chatNotifier;
        }

        public async Task Handle(
            MessageDeletedEvent notification,
            CancellationToken ct)
        {
            await _chatNotifier.MessageEditedAsync(
                notification.ConversationId,
                notification.MessageId,
                ct);
        }
    }
}
