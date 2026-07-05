using BoslaPlatform.Application.Interfaces.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BoslaPlatform.Infrastructure.Realtime
{
    [Authorize]
    public sealed class VideoHub : Hub
    {
        private readonly IAppDbContext _context;

        public VideoHub(IAppDbContext context)
        {
            _context = context;
        }

        public async Task JoinSession(Guid sessionId)
        {
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new HubException("User is not authenticated.");
            }

            var isParticipant = await _context.VideoSessionParticipants
                .AnyAsync(x => x.VideoSessionId == sessionId && x.UserId == userId);

            if (!isParticipant)
            {
                throw new HubException("You are not a participant of this session.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        }

        public async Task LeaveSession(Guid sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());
        }
    }
}
