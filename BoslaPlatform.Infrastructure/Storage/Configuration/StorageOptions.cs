using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Infrastructure.Storage.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string Provider { get; set; } = "CloudflareR2";

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string ServiceUrl { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    public string? Region { get; set; }

    public int PresignedUrlExpirationMinutes { get; set; } = 15;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryBaseDelaySeconds { get; set; } = 2;
}