using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Interfaces.Storage;

public interface IRecordingStorageSettings
{
    string BucketName { get; }
    StorageProvider Provider { get; }
    int MaxRetryAttempts { get; }
    int RetryBaseDelaySeconds { get; }

    /// <summary>
    /// Maximum lifetime of a presigned URL in minutes.
    /// RecordingAccessService clamps all requested expirations to this value.
    /// </summary>
    int PresignedUrlExpirationMinutes { get; }
}
