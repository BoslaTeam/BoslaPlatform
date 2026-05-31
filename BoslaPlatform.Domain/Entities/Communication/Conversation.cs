using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Conversations
{
    public class Conversation: AuditableEntity
    {
        public ConversationType Type { get; set; }
        public string? Title { get; set; }
        // Navigation
        public ICollection<ConversationParticipant> Participants { get; set; } = [];
        public ICollection<Message> Messages { get; set; } = [];
    }
}
