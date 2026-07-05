using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class FavoriteSpecialist : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid SpecialistId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public User User { get; set; } = null!;
        public Specialist Specialist { get; set; } = null!;
    }
}
