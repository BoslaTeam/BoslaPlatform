using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;

namespace BoslaPlatform.Domain.Models.Profile
{
    public class Education:AuditableEntity
    {
        public Guid UserId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string FieldOfStudy { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public User User { get; set; } = null!;

    }
}
