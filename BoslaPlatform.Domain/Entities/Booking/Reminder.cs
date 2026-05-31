using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Reminder: AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReminderTime { get; set; }
        public bool IsSent { get; set; } = false;

        // Navigation
        public Appointment Appointment { get; set; } = null!;
    }
}
