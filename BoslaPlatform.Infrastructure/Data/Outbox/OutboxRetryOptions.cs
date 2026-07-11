namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Options for the exponential back-off retry policy used by
/// <see cref="BackgroundJobs.OutboxDispatcherService"/>.
/// Bound from the "OutboxRetry" configuration section.
/// </summary>
public class OutboxRetryOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "OutboxRetry";

    /// <summary>
    /// Maximum number of retry attempts per message.
    /// When reached, the message is permanently failed (NextRetryUtc = null)
    /// and awaits future Dead Letter Queue migration.
    /// Default: 5.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Base delay in seconds for the exponential back-off formula:
    /// delay = min(BaseDelaySeconds × 2^(RetryCount−1), MaxDelayMinutes × 60)
    /// Default: 30.
    /// </summary>
    public int BaseDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay in minutes before a retry attempt.
    /// Caps the exponential growth so failed messages are revisited at least
    /// every MaxDelayMinutes even after many retries.
    /// Default: 30.
    /// </summary>
    public int MaxDelayMinutes { get; set; } = 30;
}
