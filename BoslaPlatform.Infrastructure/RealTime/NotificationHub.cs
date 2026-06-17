using BoslaPlatform.Application.Interfaces.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoslaPlatform.Infrastructure.RealTime
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
    }
}
