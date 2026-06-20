using BoslaPlatform.Application.Features.Notifications.DTOs;
using System.Threading.Tasks;

namespace BoslaPlatform.Application.Interfaces.Communication
{
    public interface INotificationClient
    {
        Task ReceiveNotification(NotificationDto notification);
    }
}
