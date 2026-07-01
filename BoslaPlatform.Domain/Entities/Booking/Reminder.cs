using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;

    namespace BoslaPlatform.Domain.Models.Booking
    {
        public class Reminder: AuditableEntity
        {
            public Guid AppointmentId { get; set; }
            public Guid UserId { get; set; }
            public DateTime ReminderTime { get; set; }
            public bool IsSent { get; set; } = false;
            public string Message { get; set; } = null!;
        // Navigation
            public Appointment Appointment { get; set; } = null!;
            public User User { get; set; } = null!;

        }
    }
