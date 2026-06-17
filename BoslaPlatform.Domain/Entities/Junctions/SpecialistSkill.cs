using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Lookup;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class SpecialistSkill
    {
        public Guid SpecialistId { get; set; }
        public Guid SkillId { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
