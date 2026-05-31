using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Junctions;

namespace BoslaPlatform.Domain.Models.Lookup
{
    public class Industry:Entity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<SpecialistIndustry> SpecialistIndustries { get; set; } = [];
        public ICollection<UserIndustry> UserIndustries { get; set; } = [];
    }
}
