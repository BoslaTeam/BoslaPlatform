namespace BoslaPlatform.Infrastructure.Data.Outbox;

/// <summary>
/// Options for the <see cref="BackgroundJobs.OutboxDispatcherService"/>.
/// Bound from the "OutboxDispatcher" configuration section.
/// </summary>
public class OutboxDispatcherOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "OutboxDispatcher";

    /// <summary>
    /// Maximum number of pending outbox messages to read per processing cycle.
    /// Default: 50.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Interval in seconds between polling cycles when no pending messages are found.
    /// Default: 10.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 10;
}
