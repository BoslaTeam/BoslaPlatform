using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoslaPlatform.Infrastructure.Realtime
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        public async Task JoinConversation(
            Guid conversationId)
        { 
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }

        public async Task LeaveConversation(
            Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                conversationId.ToString());
        }
    }
}
