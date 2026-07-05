using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Lookup;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class UserExpertise
    {
        public Guid UserId { get; set; }
        public Guid ExpertiseId { get; set; }
        public Expertise Expertise { get; set; } = null!;
        public User User { get; set; } = null!;

    }
}
