using BoslaPlatform.Application.Features.Notifications.DTOs;
using System;
using System.Threading.Tasks;

namespace BoslaPlatform.Application.Interfaces.Communication
{
    public interface INotificationSender
    {
        Task SendToUserAsync(Guid userId, NotificationDto notification,CancellationToken ct = default);
    }
}
