using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Lookup;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class SpecialistExpertise
    {
        public Guid SpecialistId { get; set; }
        public Guid ExpertiseId { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public Expertise Expertise { get; set; } = null!;
    }
}
