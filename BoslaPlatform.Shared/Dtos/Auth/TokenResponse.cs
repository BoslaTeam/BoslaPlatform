namespace BoslaPlatform.Shared.Dtos.Auth
{
    public class TokenResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime ExpiresOnUtc { get; init; }
    }
}
