using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoslaPlatform.Domain.Models
{
    public class SpecialistEmbedding: AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public string EmbeddingVector { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;

        // Navigation
        public Specialist Specialist { get; set; } = null!;
    }
}
