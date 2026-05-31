using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.ValueObjects;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Availability: AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        // Navigation
        public Specialist Specialist { get; set; } = null!;
    }
}
