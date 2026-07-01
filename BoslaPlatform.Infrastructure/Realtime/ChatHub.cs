using BoslaPlatform.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoslaPlatform.Infrastructure.Realtime
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private readonly UserPresenceTracker _presence;

        public ChatHub(UserPresenceTracker presence)
        {
            _presence = presence;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (userId is not null)
            {
                var (becameOnline, _) = _presence.UserConnected(
                    userId, Context.ConnectionId);

                if (becameOnline)
                {
                    // Tell everyone else this user just came online (incremental update)
                    await Clients.Others.SendAsync(
                        SignalREvents.PresenceChanged,
                        new { UserId = userId, IsOnline = true, LastSeen = (string?)null });
                }

                // Send the full snapshot of currently online users ONLY to the
                // newly connected client so it can hydrate its presence store.
                // GetOnlineUsers() returns a deduplicated HashSet — multi-device safe.
                var onlineUserIds = _presence.GetOnlineUsers();
                await Clients.Caller.SendAsync(
                    SignalREvents.OnlineUsersSnapshot,
                    onlineUserIds);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var (_, becameOffline) = _presence.UserDisconnected(
                Context.ConnectionId);

            if (becameOffline)
            {
                var userId = Context.UserIdentifier;

                if (userId is not null)
                {
                    await Clients.Others.SendAsync(
                        SignalREvents.PresenceChanged,
                        new { UserId = userId, IsOnline = false, LastSeen = DateTime.UtcNow.ToString("O") });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(Guid conversationId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }
    }
}
