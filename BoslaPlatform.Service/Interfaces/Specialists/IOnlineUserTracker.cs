namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface IOnlineUserTracker
    {
        Task UserConnectedAsync(Guid userId);

        Task UserDisconnectedAsync(Guid userId);

        bool IsOnline(Guid userId);

        IReadOnlyCollection<Guid> GetOnlineUsers();
    }
}
