using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application.Settings;

/// <summary>
/// Configures the recording retention and deletion policy.
/// Deletion is not yet implemented — these settings prepare the architecture.
/// </summary>
public sealed class RecordingRetentionOptions
{
    public const string SectionName = "RecordingRetention";

    /// <summary>
    /// Number of days to retain recordings before marking them as expired.
    /// Default: 365 days.
    /// </summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// When true, expired recordings will be soft-deleted (DeletedAtUtc set).
    /// The physical file in object storage is NOT removed.
    /// </summary>
    public bool EnableSoftDelete { get; set; } = false;

    /// <summary>
    /// When true, soft-deleted recordings will be permanently removed from object storage.
    /// Requires EnableSoftDelete = true. Reserved for future implementation.
    /// </summary>
    public bool EnableHardDelete { get; set; } = false;
}
