using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;

namespace BoslaPlatform.Domain.Models
{
    public class SearchInteraction:AuditableEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string RawQuery { get; set; } = string.Empty;
        public string? ExtractedIntent { get; set; }
        public string? ResultSpecialistIds { get; set; }
        public Guid? ClickedSpecialistId { get; set; }

        public Specialist? ClickedSpecialist { get; set; }

    }
}
