namespace BoslaPlatform.Application.Interfaces.Storage;

/// <summary>
/// Configuration settings for recording storage (Amazon S3).
/// Values are read from AgoraSettings in the Infrastructure layer.
/// </summary>
public interface IRecordingStorageSettings
{
    /// <summary>
    /// The Amazon S3 bucket where Agora Cloud Recording stores recording files.
    /// </summary>
    string RecordingBucketName { get; }

    /// <summary>
    /// Maximum lifetime of a presigned URL in minutes.
    /// RecordingAccessService clamps all requested expirations to this value.
    /// </summary>
    int PresignedUrlExpirationMinutes { get; }
}
