using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Models.Communication
{
    public class UserNotificationPreference : BaseEntity
    {
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public bool Enabled { get; set; } = true;

        public User User { get; set; } = null!;
    }
}
