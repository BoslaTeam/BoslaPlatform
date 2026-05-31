using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Video;

namespace BoslaPlatform.Domain.Models
{
    public class SessionTranscript: AuditableEntity
    {
        public Guid VideoSessionId { get; set; }
        public string TranscriptText { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        // Navigation
        public VideoSession VideoSession { get; set; } = null!;
        public SessionSummary? Summary { get; set; }
    }
}
