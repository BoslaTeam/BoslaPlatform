using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class MessageSentEvent : DomainEvent
    {
        public MessageSentEvent(
            Guid messageId,
            Guid conversationId,
            Guid senderId)
        {
            MessageId = messageId;
            ConversationId = conversationId;
            SenderId = senderId;
        }
        public Guid MessageId { get; }
        public Guid ConversationId { get; }
        public Guid SenderId { get; }
    }
}
