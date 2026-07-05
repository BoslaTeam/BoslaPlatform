using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;

namespace BoslaPlatform.Domain.Models.Profile
{
    public class SpecialistExperience : AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateOnly FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? Description { get; set; }

        // Navigation
        public Specialist Specialist { get; set; } = null!;
    }
}
