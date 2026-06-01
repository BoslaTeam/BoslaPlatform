namespace BoslaPlatform.Shared.Dtos.Auth
{
    public sealed record RefreshTokenRequest(
        string AccessToken,
        string RefreshToken);
}
