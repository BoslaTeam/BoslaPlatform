using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Conversations.Dtos
{
    public sealed class ConversationParticipantDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public ParticipantRole Role { get; set; }
    }
}
