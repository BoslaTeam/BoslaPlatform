namespace BoslaPlatform.Application.Interfaces.Authentication
{
    public interface IUser
    {
        Guid? Id { get; }
        string? Email { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
