using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.ValueObjects;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Availability: AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public DateTimeOffset Start { get; private set; }
        public DateTimeOffset End { get; private set; }
        // Navigation
        public Specialist Specialist { get; set; } = null!;
    }
}
