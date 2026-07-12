using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application.Settings;

/// <summary>
/// Configures the background reconciliation job that re-tries stuck recordings.
/// </summary>
public sealed class RecordingReconciliationOptions
{
    public const string SectionName = "RecordingReconciliation";

    /// <summary>
    /// Maximum number of retry attempts before a recording is permanently cancelled.
    /// Default: 5.
    /// </summary>
    [Range(1, 50)]
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay in seconds for exponential backoff.
    /// Retry N uses delay = BaseBackoffSeconds * 2^(N-1).
    /// Default: 60 seconds (1 minute).
    /// </summary>
    [Range(1, 3600)]
    public int BaseBackoffSeconds { get; set; } = 60;

    /// <summary>
    /// How often (in seconds) the reconciliation job polls for stuck recordings.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    [Range(10, 86400)]
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of recordings to process per reconciliation pass.
    /// Prevents runaway job execution. Default: 20.
    /// </summary>
    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;
}
