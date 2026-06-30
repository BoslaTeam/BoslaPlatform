using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Reminders
{
    public sealed class ReminderDueEvent : DomainEvent
    {
        public Guid ReminderId { get; }
        public Guid AppointmentId { get; }
        public Guid UserId { get; }
        public string Message { get; }

        public ReminderDueEvent(
            Guid reminderId,
            Guid appointmentId,
            Guid userId,
            string message)
        {
            ReminderId = reminderId;
            AppointmentId = appointmentId;
            UserId = userId;
            Message = message;
        }
    }
}
