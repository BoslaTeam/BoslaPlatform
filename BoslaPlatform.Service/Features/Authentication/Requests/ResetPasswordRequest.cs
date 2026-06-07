namespace BoslaPlatform.Application
{
    public sealed record ResetPasswordRequest(
        string Email,
        string Token,
        string NewPassword);
}
