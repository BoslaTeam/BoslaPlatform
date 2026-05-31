namespace BoslaPlatform.Infrastructure.Settings
{
    public class JwtSettings
    {
        public string SecretKey { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int TokenExpirationInMinutes { get; init; }
        public int RefreshTokenExpirationInDays { get; init; }
    }
}
