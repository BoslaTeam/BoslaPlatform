using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class MessageEditedEvent : DomainEvent
    {
        public MessageEditedEvent(
            Guid messageId,
            Guid conversationId)
        {
            MessageId = messageId;
            ConversationId = conversationId;
        }
        public Guid MessageId { get; }
        public Guid ConversationId { get; }
    }
}
