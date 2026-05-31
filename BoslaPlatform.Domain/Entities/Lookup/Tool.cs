using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Junctions;

namespace BoslaPlatform.Domain.Models.Lookup
{
    public class Tool:Entity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<SpecialistTool> SpecialistTools { get; set; } = [];
    }
}
