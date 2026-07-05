namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword);
}
