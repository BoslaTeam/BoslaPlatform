using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage;

/// <summary>
/// Recording storage settings read from AgoraSettings.
/// Agora Cloud Recording uploads directly to Amazon S3, so the S3 bucket
/// and access configuration are managed inside AgoraSettings.
/// </summary>
public sealed class RecordingStorageSettings : IRecordingStorageSettings
{
    private readonly AgoraSettings _settings;

    public RecordingStorageSettings(IOptions<AgoraSettings> options)
    {
        _settings = options.Value;
    }

    public string RecordingBucketName => _settings.StorageBucket;

    public int PresignedUrlExpirationMinutes => 15;
}
