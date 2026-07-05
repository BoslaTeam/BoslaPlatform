using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Identity;

namespace BoslaPlatform.Domain.Models.Video
{
    public class VideoSessionParticipant : AuditableEntity
    {
        private VideoSessionParticipant()
        {
        }

        public Guid VideoSessionId { get; private set; }

        public Guid UserId { get; private set; }

        public long AgoraUid { get; private set; }

        public VideoParticipantRole Role { get; private set; }

        public DateTime? JoinedAt { get; private set; }

        public DateTime? LeftAt { get; private set; }

        public VideoSession VideoSession { get; private set; } = null!;

        public User User { get; private set; } = null!;

        public static VideoSessionParticipant Create(
            Guid videoSessionId,
            Guid userId,
            long agoraUid,
            VideoParticipantRole role)
        {
            return new VideoSessionParticipant
            {
                VideoSessionId = videoSessionId,
                UserId = userId,
                AgoraUid = agoraUid,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }

        public void MarkLeft()
        {
            if (LeftAt is not null)
                return;

            LeftAt = DateTime.UtcNow;
        }

        public void ClearLeaveState()
        {
            LeftAt = null;
        }
    }
}
