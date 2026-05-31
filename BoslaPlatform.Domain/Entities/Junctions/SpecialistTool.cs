using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;

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
