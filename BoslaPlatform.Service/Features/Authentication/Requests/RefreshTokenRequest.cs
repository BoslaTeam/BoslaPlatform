namespace BoslaPlatform.Application
{
    public sealed record RefreshTokenRequest(
        string AccessToken,
        string RefreshToken);
}
