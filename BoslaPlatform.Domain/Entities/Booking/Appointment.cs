using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Appointment: AuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid SpecialistId { get; set; }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public DateTime BookedAt { get; set; }
        public string? SessionTopic { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation
        public Specialist Specialist { get; set; } = null!;
        public User User { get; set; } = null!;
        public Payment? Payment { get; set; }
        public Review? Review { get; set; }
        public ScreenRecording? ScreenRecording { get; set; }
        public VideoSession? VideoSession { get; set; }
        public ICollection<Reminder> Reminders { get; set; } = [];
        public ICollection<AppointmentStatusHistory> StatusHistory { get; set; } = [];
        public SessionSummary? SessionSummary { get; set; }
    }
}
