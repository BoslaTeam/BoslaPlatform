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
        public int MaxIdleTime { get; init; } = 30;

        public int StreamTypes { get; init; } = 2;

        public int ChannelType { get; init; }

        public int VideoStreamType { get; init; }

        public int SubscribeUidGroup { get; init; }
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
