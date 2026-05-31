using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Models.Profile
{
    public class SocialLink:AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
