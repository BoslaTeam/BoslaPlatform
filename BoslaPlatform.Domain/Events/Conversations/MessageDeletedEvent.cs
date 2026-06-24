using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class MessageDeletedEvent : DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ConversationId { get; }

        public MessageDeletedEvent(
            Guid messageId,
            Guid conversationId)
        {
            MessageId = messageId;
            ConversationId = conversationId;
        }
    }
}
