using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Lookup;

namespace BoslaPlatform.Domain.Models.Junctions
{
    public class SpecialistTool
    {
        public Guid SpecialistId { get; set; }
        public Guid ToolId { get; set; }
        public Specialist Specialist { get; set; } = null!;
        public Tool Tool { get; set; } = null!;
    }
}
