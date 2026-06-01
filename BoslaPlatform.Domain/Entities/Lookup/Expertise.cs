using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Junctions;

namespace BoslaPlatform.Domain.Models.Lookup
{
    public class Expertise:BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<SpecialistExpertise> SpecialistExpertise { get; set; } = [];
        public ICollection<UserExpertise> UserExpertise { get; set; } = [];
    }
}
