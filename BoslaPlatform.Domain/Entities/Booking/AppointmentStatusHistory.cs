using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class AppointmentStatusHistory: AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public AppointmentStatus? OldStatus { get; set; }
        public AppointmentStatus NewStatus { get; set; }
        public string? Reason { get; set; }

        // Navigation
        public Appointment Appointment { get; set; } = null!;
        public User ChangedByUser { get; set; }

    }
}
