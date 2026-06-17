using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Communication
{
    public class ConversationParticipant : AuditableEntity
    {
        private ConversationParticipant() { }
        public Guid ConversationId { get; private set; }
        public Guid UserId { get; private set; }
        //public ParticipantRole? Role { get; private set; }
        public Conversation Conversation { get; private set; } = null!;
        public User User { get; private set; } = null!;

        public static ConversationParticipant Create(
            Guid conversationId,
            Guid userId)
        {
            return new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = userId,
            };
        }
        // Method to change the role of a participant
        //public void ChangeRole(ParticipantRole role)
        //{
        //    Role = role;
        //}

}
}
