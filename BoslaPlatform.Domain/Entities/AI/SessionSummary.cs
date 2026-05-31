using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoslaPlatform.Domain.Models
{
    public class SessionSummary: AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public Guid? TranscriptId { get; set; }
        public string KeyTakeaways { get; set; } = string.Empty;
        public string ActionItemsForUser { get; set; } = string.Empty;
        public string ActionItemsForSpec { get; set; } = string.Empty;
        public string LlmProvider { get; set; } = string.Empty;
        public SummaryStatus Status { get; set; } = SummaryStatus.Pending;

        // Navigation
        public Appointment Appointment { get; set; } = null!;
        public SessionTranscript? Transcript { get; set; }
    }
}
