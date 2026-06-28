using BoslaPlatform.Application.Interfaces.Specialists;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BoslaPlatform.Application.Features.Specialists.Services
{
    public sealed class OnlineUserTracker : IOnlineUserTracker
    {
        private readonly IMemoryCache _cache;

        private static readonly ConcurrentDictionary<Guid, int> Connections = new();

        public OnlineUserTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task UserConnectedAsync(Guid userId)
        {
            Connections.AddOrUpdate(
                userId,
                1,
                (_, count) => count + 1);

            _cache.Set(
                GetKey(userId),
                true,
                TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        public Task UserDisconnectedAsync(Guid userId)
        {
            if (Connections.TryGetValue(userId, out var count))
            {
                if (count <= 1)
                {
                    Connections.TryRemove(userId, out _);

                    _cache.Remove(GetKey(userId));
                }
                else
                {
                    Connections[userId] = count - 1;
                }
            }

            return Task.CompletedTask;
        }

        public bool IsOnline(Guid userId)
            => _cache.TryGetValue(GetKey(userId), out _);

        public IReadOnlyCollection<Guid> GetOnlineUsers()
            => Connections.Keys.ToList();

        private static string GetKey(Guid userId)
            => $"online:{userId}";
    }
}
