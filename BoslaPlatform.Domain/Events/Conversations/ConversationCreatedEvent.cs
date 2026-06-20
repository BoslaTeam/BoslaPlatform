using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class ConversationCreatedEvent : DomainEvent
    {
        public ConversationCreatedEvent(
            Guid conversationId,
            Guid createdBy)
        {
            ConversationId = conversationId;
            CreatedBy = createdBy;
        }
        public Guid ConversationId { get; }
        public Guid CreatedBy { get; }
    }
}
