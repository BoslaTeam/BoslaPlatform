
using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;

using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Communication;
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
        public decimal SessionPrice { get; private set; }

        // Navigation Properties
        public User User { get; private set; } = null!;
        public Specialist Specialist { get; private set; } = null!;

        private readonly List<AppointmentStatusHistory> _statusHistory = new();
        public virtual IReadOnlyCollection<AppointmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

        public virtual VideoSession? VideoSession { get; private set; }

       public Payment? Payment { get; private set; }

        public virtual SessionSummary? SessionSummary { get; private set; }
        public virtual Review? Review { get; private set; }

        public virtual ScreenRecording? ScreenRecording { get; private set; }


        private readonly List<Reminder> _reminders = new();
        public virtual IReadOnlyCollection<Reminder> Reminders => _reminders.AsReadOnly();
        public virtual Conversation? Conversation { get; private set; }

        private Appointment() { }

        // Factory Method
        public static Appointment Schedule(
            Guid specialistId,
            Guid userId,
            DateTimeOffset start,
            DateTimeOffset end,
            string? sessionTopic,
            string? notes,
            decimal sessionPrice)
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                SpecialistId = specialistId,
                UserId = userId,
                SessionPrice = sessionPrice,
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


            return appointment;
        }


        public Result MarkAsPaid()
        {
            if (Status == AppointmentStatus.Paid)
                return Result.Success();

            if (Status != AppointmentStatus.Pending && Status != AppointmentStatus.Confirmed)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "Only pending or confirmed appointments can be marked as paid."));

            UpdateStatus(AppointmentStatus.Paid, Guid.Empty, "Payment completed.");
            return Result.Success();
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
            if (Status != AppointmentStatus.Confirmed && Status != AppointmentStatus.Paid)
                return Result.Failure(Error.Validation(
                    "Appointment.InvalidStatusTransition",
                    "Only confirmed or paid appointments can be marked as completed."));

            UpdateStatus(AppointmentStatus.Completed, specialistId, "Session completed successfully.");
            AddDomainEvent(new AppointmentCompletedEvent(Id, SpecialistId, UserId));
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
        public void AttachVideoSession(VideoSession videoSession)
        {
            VideoSession = videoSession;
        }
        public Result CanStartVideoSession(DateTimeOffset currentTime)
        {
            var allowedStart = Start.AddMinutes(-15);

            var allowedEnd = Start.AddMinutes(15);

            if (currentTime < allowedStart)
            {
                return Error.Validation(
                    "Appointment.TooEarly",
                    "Video session cannot start yet.");
            }

            if (currentTime > allowedEnd)
            {
                return Error.Validation(
                    "Appointment.StartWindowExpired",
                    "Video session start window has expired.");
            }

            return Result.Success();
        }
    }
}