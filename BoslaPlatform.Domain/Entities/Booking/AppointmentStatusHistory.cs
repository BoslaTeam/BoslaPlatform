using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class AppointmentStatusHistory: AuditableEntity
    {
        public Guid AppointmentId { get; private set; }
        public AppointmentStatus OldStatus { get; private set; }
        public AppointmentStatus NewStatus { get; private set; }
        public string? Reason { get; private set; }


        public Appointment Appointment { get; private set; } = null!;
        public User ChangedByUser { get; private set; } = null!; 

        private AppointmentStatusHistory() { }

        public AppointmentStatusHistory(Guid appointmentId, AppointmentStatus oldStatus, AppointmentStatus newStatus, string? reason)
        {
            AppointmentId = appointmentId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Reason = reason;
        }

    }
}
