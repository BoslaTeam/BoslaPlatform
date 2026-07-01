using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class EmbeddingsStatusDto
    {
        public int TotalSpecialists { get; set; }
        public int EmbeddedCount { get; set; }
        public int PendingCount { get; set; }
        public DateTime? LastRebuildAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
