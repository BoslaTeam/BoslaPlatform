using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage;

public sealed class RecordingStorageSettings : IRecordingStorageSettings
{
    private readonly StorageOptions _options;

    public RecordingStorageSettings(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public string BucketName => _options.BucketName;

    public StorageProvider Provider => Enum.TryParse<StorageProvider>(
        _options.Provider,
        ignoreCase: true,
        out var provider)
            ? provider
            : StorageProvider.CloudflareR2;

    public int MaxRetryAttempts => Math.Max(1, _options.MaxRetryAttempts);

    public int RetryBaseDelaySeconds => Math.Max(1, _options.RetryBaseDelaySeconds);

    public int PresignedUrlExpirationMinutes => Math.Max(1, _options.PresignedUrlExpirationMinutes);
}
