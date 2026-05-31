using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class SpecialistIndustry
    {
        public Guid SpecialistId { get; set; }
        public Guid IndustryId { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public Industry Industry { get; set; } = null!;
    }
}
