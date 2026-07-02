using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;

namespace BoslaPlatform.Domain.Models
{
    public class SpecialistEmbedding: AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public string EmbeddingVector { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTimeOffset? LastEmbeddedAt { get; set; }

    // Navigation
    public Specialist Specialist { get; set; } = null!;
    }
}
