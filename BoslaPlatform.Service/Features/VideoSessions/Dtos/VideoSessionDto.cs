using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.VideoSessions.Dtos
{
    public sealed class VideoSessionDto
    {
        public Guid Id { get; set; }
        public Guid? AppointmentId { get; set; }
        public string ChannelName { get; set; } = string.Empty;
        public VideoSessionStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public List<VideoSessionParticipantDto> Participants { get; set; } = [];
    }
}
