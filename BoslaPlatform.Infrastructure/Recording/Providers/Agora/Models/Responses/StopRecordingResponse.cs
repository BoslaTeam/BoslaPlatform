namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Responses
{
    internal sealed record StopRecordingResponse
    {
        public string ResourceId { get; init; } = string.Empty;

        public string Sid { get; init; } = string.Empty;

        public AgoraServerResponse? ServerResponse { get; init; }
    }

    internal sealed record AgoraServerResponse
    {
        public AgoraFileInfo[]? FileList { get; init; }

        public string? UploadingStatus { get; init; }
    }

    internal sealed record AgoraFileInfo
    {
        public string? FileName { get; init; }

        public long FileSize { get; init; }

        public long SliceStartTime { get; init; }

        public string? Status { get; init; }
    }
}
