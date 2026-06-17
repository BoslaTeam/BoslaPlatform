using BoslaPlatform.Domain.Common;


namespace BoslaPlatform.Domain.Events.Apoointments
{
    public class AppointmentScheduledEvent : DomainEvent
    {
        public Guid AppointmentId { get; }
        public Guid SpecialistId { get; }
        public Guid UserId { get; }
        public DateTimeOffset Start { get; }

        public AppointmentScheduledEvent(Guid appointmentId, Guid specialistId, Guid userId, DateTimeOffset start)
        {
            AppointmentId = appointmentId;
            SpecialistId = specialistId;
            UserId = userId;
            Start = start;
        }
    }
}
