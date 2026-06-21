using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    public sealed class VideoSessionParticipantDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public VideoParticipantRole Role { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
