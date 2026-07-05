using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Specialists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BoslaPlatform.Infrastructure.RealTime
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly IOnlineUserTracker _tracker;

        public NotificationHub(IOnlineUserTracker tracker)
        {
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _tracker.UserConnectedAsync(userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim =
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _tracker.UserDisconnectedAsync(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
