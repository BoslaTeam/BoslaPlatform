using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class MessageSentEvent : DomainEvent
    {
        public Guid MessageId { get; }
        public Guid ConversationId { get; }
        public Guid SenderId { get; }
        public string MessageText { get; }

        public MessageSentEvent(
            Guid messageId,
            Guid conversationId,
            Guid senderId,
            string messageText)
        {
            MessageId = messageId;
            ConversationId = conversationId;
            SenderId = senderId;
            MessageText = messageText;
        }
    }
}
