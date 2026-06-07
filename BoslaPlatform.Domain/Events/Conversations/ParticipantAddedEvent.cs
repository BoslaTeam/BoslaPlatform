using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class ParticipantAddedEvent : DomainEvent
    {
        public ParticipantAddedEvent(
            Guid conversationId,
            Guid userId)
        {
            ConversationId = conversationId;
            UserId = userId;
        }

        public Guid ConversationId { get; }

        public Guid UserId { get; }
    }
}
