using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class VideoSessionEndedEvent : DomainEvent
    {
        public VideoSessionEndedEvent(Guid sessionId, Guid appointmentId)
        {
            SessionId = sessionId;
            AppointmentId = appointmentId;
        }

        public Guid SessionId { get; }
        public Guid AppointmentId { get; }
    }
}
