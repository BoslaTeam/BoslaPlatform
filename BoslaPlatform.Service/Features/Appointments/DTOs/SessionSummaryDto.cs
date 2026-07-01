using System;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Appointments.DTOs
{
    public class SessionSummaryDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid? TranscriptId { get; set; }
        public string KeyTakeaways { get; set; } = string.Empty;
        public string ActionItemsForUser { get; set; } = string.Empty;
        public string ActionItemsForSpec { get; set; } = string.Empty;
        public string LlmProvider { get; set; } = string.Empty;
        public SummaryStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTimeOffset? LastModifiedUtc { get; set; }
        public Guid? LastModifiedBy { get; set; }
    }
}
