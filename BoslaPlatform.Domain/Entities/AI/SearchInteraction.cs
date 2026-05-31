using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Profile;

namespace BoslaPlatform.Domain.Models
{
    public class SearchInteraction:AuditableEntity
    {
        public Guid UserId { get; set; }
        public string RawQuery { get; set; } = string.Empty;
        public string? ExtractedIntent { get; set; }
        public string? ResultSpecialistIds { get; set; }
        public Guid? ClickedSpecialistId { get; set; }

        public Specialist? ClickedSpecialist { get; set; }
    }
}
