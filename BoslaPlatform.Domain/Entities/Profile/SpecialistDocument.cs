using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class SpecialistDocument : AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public SpecialistDocumentType Type { get; set; }
        public string Url { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;

        public Specialist Specialist { get; set; } = null!;
    }
}
