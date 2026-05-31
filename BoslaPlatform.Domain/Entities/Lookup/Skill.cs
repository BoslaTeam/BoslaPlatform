using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Junctions;

namespace BoslaPlatform.Domain.Models.Lookup
{
    public class Skill:Entity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<SpecialistSkill> SpecialistSkills { get; set; } = [];
    }
}
