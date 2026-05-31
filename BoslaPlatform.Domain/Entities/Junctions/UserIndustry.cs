using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Lookup;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class UserIndustry
    {
        public Guid UserId { get; set; }
        public Guid IndustryId { get; set; }
        public Industry Industry { get; set; } = null!;
        public User User { get; set; }

    }
}
