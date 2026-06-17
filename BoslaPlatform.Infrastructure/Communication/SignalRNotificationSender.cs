using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Infrastructure.RealTime;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace BoslaPlatform.Infrastructure.Communication
{
    public sealed class SignalRNotificationSender : INotificationSender
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public SignalRNotificationSender(IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToUserAsync(Guid userId, NotificationDto notification)
        {
            await _hubContext.Clients.User(userId.ToString()).ReceiveNotification(notification);
        }
    }
}
