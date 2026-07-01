using System.Collections.Concurrent;

namespace BoslaPlatform.Infrastructure.Realtime
{
    public sealed class UserPresenceTracker
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        private static readonly HashSet<string> Empty = [];

        public (bool becameOnline, bool becameOffline) UserConnected(
            string userId,
            string connectionId)
        {
            var connections = _userConnections
                .GetOrAdd(userId, _ => []);

            lock (connections)
            {
                var wasOffline = connections.Count == 0;
                connections.Add(connectionId);
                return (wasOffline, false);
            }
        }

        public (bool becameOnline, bool becameOffline) UserDisconnected(
            string connectionId)
        {
            foreach (var kvp in _userConnections)
            {
                var connections = kvp.Value;

                lock (connections)
                {
                    if (connections.Remove(connectionId) && connections.Count == 0)
                    {
                        _userConnections.TryRemove(
                            KeyValuePair.Create(kvp.Key, connections));
                        return (false, true);
                    }
                }
            }

            return (false, false);
        }

        public bool IsOnline(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.Count > 0;
                }
            }

            return false;
        }

        public IReadOnlySet<string> GetOnlineUsers()
        {
            var online = new HashSet<string>();

            foreach (var kvp in _userConnections)
            {
                var connections = kvp.Value;

                lock (connections)
                {
                    if (connections.Count > 0)
                    {
                        online.Add(kvp.Key);
                    }
                }
            }

            return online;
        }
    }
}
