namespace BoslaPlatform.Infrastructure.Settings
{
    /// <summary>
    /// Configuration settings for Agora service integration.
    /// These settings should be bound from the application configuration (appsettings.json).
    /// </summary>
    public class AgoraSettings
    {
        public const string SectionName = "AgoraSettings";
        /// <summary>
        /// Gets or sets the Agora Application ID.
        /// This is required to authenticate requests to the Agora platform.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Agora App Certificate.
        /// This is required to generate RTC tokens for channel access.
        /// </summary>
        public string AppCertificate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token expiration time in minutes.
        /// This controls how long generated tokens remain valid.
        /// Default is typically 24 hours (1440 minutes).
        /// </summary>
        public int TokenExpirationMinutes { get; set; } = 1440;
    }
}
