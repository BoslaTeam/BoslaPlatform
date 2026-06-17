using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;


namespace BoslaPlatform.Domain.Events.Apoointments
{
    public class AppointmentStatusChangedEvent : DomainEvent
    {
        public Guid AppointmentId { get; }
        public AppointmentStatus OldStatus { get; }
        public AppointmentStatus NewStatus { get; }
        public Guid ChangedByUserId { get; }
        public string? Reason { get; }

        public AppointmentStatusChangedEvent(Guid appointmentId, AppointmentStatus oldStatus, AppointmentStatus newStatus, Guid changedByUserId, string? reason)
        {
            AppointmentId = appointmentId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedByUserId = changedByUserId;
            Reason = reason;
        }
    }
}
