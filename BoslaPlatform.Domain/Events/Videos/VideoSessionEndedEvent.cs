using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class VideoSessionEndedEvent : DomainEvent
    {
        public VideoSessionEndedEvent(Guid sessionId, Guid appointmentId, DateTime endedAtUtc)
        {
            SessionId = sessionId;
            AppointmentId = appointmentId;
            EndedAtUtc = endedAtUtc;
        }

        public Guid SessionId { get; }
        public Guid AppointmentId { get; }
        public DateTime EndedAtUtc { get; }
    }
}
