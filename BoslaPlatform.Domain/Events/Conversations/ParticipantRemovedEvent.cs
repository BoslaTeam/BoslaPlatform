using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Conversations
{
    public sealed class ParticipantRemovedEvent : DomainEvent
    {
        public ParticipantRemovedEvent(
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
