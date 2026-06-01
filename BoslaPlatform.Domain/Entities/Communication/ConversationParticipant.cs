using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Identity;

namespace BoslaPlatform.Domain.Models.Conversations
{
    public class ConversationParticipant: AuditableEntity
    {
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public ParticipantRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; }

    }
}
