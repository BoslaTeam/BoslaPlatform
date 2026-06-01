using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Identity;

namespace BoslaPlatform.Domain.Models.Video
{
    public class VideoSessionParticipant: AuditableEntity
    {
        public Guid VideoSessionId { get; set; }
        public Guid UserId { get; set; }
        public long AgoraUid { get; set; }
        public VideoParticipantRole Role { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        // Navigation
        public VideoSession VideoSession { get; set; } = null!;
        public User User { get; set; }

    }
}
