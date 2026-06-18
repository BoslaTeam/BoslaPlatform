using MediatR;

namespace BoslaPlatform.Domain.Events.Identity
{
    public sealed class EmailVerifiedEvent(Guid userId, string email, string userName) : INotification
    {
        public Guid UserId { get; } = userId;
        public string Email { get; } = email;
        public string UserName { get; } = userName;
    }
}
