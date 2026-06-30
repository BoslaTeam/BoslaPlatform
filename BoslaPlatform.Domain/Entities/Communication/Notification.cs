using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Communication
{
    public class Notification: AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public Guid? AppointmentId { get; set; }
        public int? AppointmentStatus { get; set; }
        public User User { get; set; }

    }
}
