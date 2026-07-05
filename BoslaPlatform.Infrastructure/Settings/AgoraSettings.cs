using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Infrastructure.Settings
{
    /// <summary>
    /// Configuration settings for Agora service integration.
    /// These settings should be bound from the application configuration (appsettings.json).
    ///
    /// SECURITY NOTE on WebhookSecret:
    ///   The webhook secret is configured in the Agora Console under
    ///   Notifications → Webhook Secret. It is used to verify the HMAC-SHA256
    ///   signature that Agora attaches to every webhook request.
    ///   NEVER commit the real secret to source control.
    ///   Use environment variables or a secrets manager in production.
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

        /// <summary>
        /// Gets or sets the webhook secret used for HMAC-SHA256 signature verification.
        ///
        /// SECURITY:
        ///   This value must match the "Secret" configured in the Agora Console
        ///   under Notifications → Webhook Secret.
        ///   If empty, webhook signature verification will be skipped with a
        ///   WARNING log (should only happen in development).
        /// </summary>
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the replay attack prevention window in seconds.
        /// Webhook requests whose timestamp differs from server time by more than
        /// this many seconds will be rejected.
        /// Default: 300 seconds (5 minutes).
        /// </summary>
        public int WebhookReplayWindowSeconds { get; set; } = 300;

        public bool SkipSignatureValidation { get; set; } = false;

        // Cloud Recording REST API
        // TODO: Store CustomerId and CustomerSecret in Azure Key Vault or user secrets in production.
        //       These values are currently bound from IConfiguration and can be overridden
        //       via environment variables (e.g., AgoraSettings__CustomerId, AgoraSettings__CustomerSecret).
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerSecret { get; set; } = string.Empty;
        public string CloudRecordingBaseUrl { get; set; } = "https://api.agora.io";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;

        // Cloud Recording defaults
        public int RecordingMaxIdleTime { get; set; } = 30;
        public int RecordingStreamTypes { get; set; } = 2;

        // Cloud Recording Storage
        public int StorageVendor { get; set; }
        public int StorageRegion { get; set; }
        public string StorageBucket { get; set; } = string.Empty;
        public string StorageAccessKey { get; set; } = string.Empty;
        public string StorageSecretKey { get; set; } = string.Empty;
        public string StorageFileNamePrefix { get; set; } = string.Empty;
    }
}
