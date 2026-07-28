namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests
{
    internal sealed record StartRecordingRequest
    {
        public string Cname { get; init; } = string.Empty;

        public string Uid { get; init; } = string.Empty;

        public StartClientRequest ClientRequest { get; init; } = new();
    }

    internal sealed record StartClientRequest
    {
        public RecordingConfig RecordingConfig { get; init; } = new();

        public RecordingFileConfig RecordingFileConfig { get; init; } = new();

        public StorageConfig StorageConfig { get; init; } = new();
    }

    internal sealed record RecordingConfig
    {
        /// <summary>
        /// RTC token the cloud-recording client uses to join the channel.
        /// Required when the channel is secured with an App Certificate;
        /// null (omitted from the payload) for App ID-only channels.
        /// </summary>
        public string? Token { get; init; }

        public int MaxIdleTime { get; init; } = 30;

        public int StreamTypes { get; init; } = 2;

        public int ChannelType { get; init; }

        public int VideoStreamType { get; init; }

        public int SubscribeUidGroup { get; init; }

        /// <summary>
        /// UIDs whose audio the recorder subscribes to. Agora subscribes to
        /// nothing unless this is set, so an unset list means the recorder joins,
        /// captures no media, and is torn down once MaxIdleTime elapses.
        /// "#allstream#" subscribes to every publisher in the channel.
        /// </summary>
        public string[]? SubscribeAudioUids { get; init; }

        /// <summary>
        /// UIDs whose video the recorder subscribes to. See <see cref="SubscribeAudioUids"/>.
        /// </summary>
        public string[]? SubscribeVideoUids { get; init; }
    }

    internal sealed record RecordingFileConfig
    {
        public string[] AvFileType { get; init; } = ["hls"];
    }

    internal sealed record StorageConfig
    {
        public int Vendor { get; init; }

        public int Region { get; init; }

        public string Bucket { get; init; } = string.Empty;

        public string AccessKey { get; init; } = string.Empty;

        public string SecretKey { get; init; } = string.Empty;

        public string[] FileNamePrefix { get; init; } = [];
    }
}
