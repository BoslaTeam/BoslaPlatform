
using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;

using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared; 

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Appointment : AuditableEntity
    {
        public Guid SpecialistId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTimeOffset Start { get; private set; }
        public DateTimeOffset End { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public string? SessionTopic { get; private set; }
        public string? Notes { get; private set; }
        public string? CancellationReason { get; private set; }

        // Navigation Properties
        public User User { get; private set; } = null!;
        public Specialist Specialist { get; private set; } = null!;

        private readonly List<AppointmentStatusHistory> _statusHistory = new();
        public IReadOnlyCollection<AppointmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

        // 1. لـ VideoSessionConfiguration (رأس برأس One-to-One)
        public VideoSession? VideoSession { get; private set; }

        // 2. لـ ReminderConfiguration (رأس بأطراف One-to-Many)
        public ICollection<Reminder> Reminders { get; private set; } = new List<Reminder>();

        // 3. لـ ReviewConfiguration (رأس برأس One-to-One)
        public Review? Review { get; private set; }

        // 4. لـ PaymentConfiguration (رأس برأس One-to-One)
        public Payment? Payment { get; private set; }

        // 5. لـ SessionSummaryConfiguration (رأس برأس One-to-One)
        public SessionSummary? SessionSummary { get; private set; }

        // 6. لـ ScreenRecordingConfiguration (رأس برأس One-to-One)
        public ScreenRecording? ScreenRecording { get; private set; }

        private Appointment() { }

        // Factory Method
        public static Appointment Schedule(
            Guid specialistId,
            Guid userId,
            DateTimeOffset start,
            DateTimeOffset end,
            string? sessionTopic,
            string? notes)
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                SpecialistId = specialistId,
                UserId = userId,
                Start = start,
                End = end,
                Status = AppointmentStatus.Pending,
                SessionTopic = sessionTopic,
                Notes = notes
            };

            appointment._statusHistory.Add(new AppointmentStatusHistory(
                appointment.Id,
                AppointmentStatus.Pending,
                AppointmentStatus.Pending,
                "Initial booking request."));

            appointment.AddDomainEvent(new AppointmentScheduledEvent(appointment.Id, specialistId, userId, start));

            return appointment;
        }


        public Result Confirm(Guid specialistId)
        {
            if (Status != AppointmentStatus.Pending)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "Only pending appointments can be confirmed."));

            UpdateStatus(AppointmentStatus.Confirmed, specialistId, "Appointment confirmed by specialist.");
            return Result.Success();
        }

        public Result Cancel(Guid cancelledByUserId, string reason)
        {
            if (Status == AppointmentStatus.Cancelled || Status == AppointmentStatus.Completed)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "This appointment cannot be cancelled in its current state."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(Error.Validation(
                    "Appointment.CancellationReasonRequired",
                    "A reason must be provided for cancellation."));

            CancellationReason = reason;
            UpdateStatus(AppointmentStatus.Cancelled, cancelledByUserId, reason);
            return Result.Success();
        }

        public Result Reschedule(Guid changedByUserId, DateTimeOffset newStart, DateTimeOffset newEnd, string reason)
        {
            if (Status == AppointmentStatus.Cancelled || Status == AppointmentStatus.Completed)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "Cannot reschedule a completed or cancelled appointment."));

            if (newStart >= newEnd)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidTimeRange",
                    "Start time must be before end time."));

            Start = newStart;
            End = newEnd;

            UpdateStatus(AppointmentStatus.Rescheduled, changedByUserId, $"Rescheduled: {reason}");
            return Result.Success();
        }

        public Result Complete(Guid specialistId)
        {
            if (Status != AppointmentStatus.Confirmed)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "Only confirmed appointments can be marked as completed."));

            UpdateStatus(AppointmentStatus.Completed, specialistId, "Session completed successfully.");
            return Result.Success();
        }

        private void UpdateStatus(AppointmentStatus newStatus, Guid changedByUserId, string? reason)
        {
            var oldStatus = Status;
            Status = newStatus;

            _statusHistory.Add(new AppointmentStatusHistory(Id, oldStatus, newStatus, reason));

            AddDomainEvent(new AppointmentStatusChangedEvent(Id, oldStatus, newStatus, changedByUserId, reason));
        }
        public void UpdateNotes(string notes)
        {
            Notes = notes;
        }
    }
}