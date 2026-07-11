namespace BoslaPlatform.Infrastructure.Settings;

public sealed class RateLimitingSettings
{
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = [];
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}
