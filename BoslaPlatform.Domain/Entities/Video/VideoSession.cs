using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;

namespace BoslaPlatform.Domain.Models.Video
{
    public class VideoSession:AuditableEntity
    {
        public Guid? AppointmentId { get; set; }
        public VideoSessionType Type { get; set; }
        public string AgoraChannelName { get; set; } = string.Empty;
        public string AgoraAppId { get; set; } = string.Empty;
        public VideoSessionStatus Status { get; set; } = VideoSessionStatus.Waiting;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? AgoraRecordingId { get; set; }
        public string? AgoraRecordingSid { get; set; }
        public string? RecordingUrl { get; set; }

        // Navigation
        public Appointment? Appointment { get; set; }
        public ICollection<VideoSessionParticipant> Participants { get; set; } = [];
    }
}
