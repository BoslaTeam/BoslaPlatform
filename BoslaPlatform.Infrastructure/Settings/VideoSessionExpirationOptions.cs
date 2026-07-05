namespace BoslaPlatform.Infrastructure.Settings
{
    public class VideoSessionExpirationOptions
    {
        public int BatchSize { get; set; } = 20;
        public int PollingIntervalSeconds { get; set; } = 60;
    }
}
