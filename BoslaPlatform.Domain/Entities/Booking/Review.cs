using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Profile;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Review: AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public Guid ReviewerId { get; set; }
        public Guid SpecialistId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        // Navigation
        public Appointment Appointment { get; set; } = null!;
        public Specialist Specialist { get; set; } = null!;
    }
}
