using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Infrastructure.Storage.Cloudflare;

public sealed class TemporaryFileCleanerOptions
{
    public const string SectionName = "TemporaryFileCleaner";

    [Required]
    [Range(1, 1440)]
    public int RetentionMinutes { get; set; } = 60;

    [Required]
    [Range(1, 3600)]
    public int PollingIntervalSeconds { get; set; } = 300;

    [Required]
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;
}