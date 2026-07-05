using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Apoointments
{
    public sealed class AppointmentCompletedEvent : DomainEvent
    {
        public Guid AppointmentId { get; }
        public Guid SpecialistId { get; }
        public Guid UserId { get; }

        public AppointmentCompletedEvent(Guid appointmentId, Guid specialistId, Guid userId)
        {
            AppointmentId = appointmentId;
            SpecialistId = specialistId;
            UserId = userId;
        }
    }
}
