namespace BoslaPlatform.Application
{
    public sealed record LoginRequest(
        string Email,
        string Password);
}
