using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.ValueObjects;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Availability : AuditableEntity
    {
        public Guid SpecialistId { get; private set; }

        public DateTimeOffset Start { get; private set; }

        public DateTimeOffset End { get; private set; }

        public Specialist Specialist { get; set; } = null!;

        public bool IsBooked { get; private set; } = false;

        public static Availability Create(
            Guid specialistId,
            DateTimeOffset start,
            DateTimeOffset end)
        {
            return new Availability
            {
                SpecialistId = specialistId,
                Start = start,
                End = end,
                IsBooked = false
            };
        }
    }
}
