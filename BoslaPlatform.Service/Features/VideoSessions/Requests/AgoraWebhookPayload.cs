using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public sealed class AgoraWebhookPayload
    {
        [JsonPropertyName("channelName")]
        public string ChannelName { get; init; } = string.Empty;

        [JsonPropertyName("uid")]
        public long Uid { get; init; }

        [JsonPropertyName("ts")]
        public long Ts { get; init; }

        [JsonPropertyName("sid")]
        public string Sid { get; init; } = string.Empty;

        [JsonPropertyName("sequence")]
        public int Sequence { get; init; }

        [JsonPropertyName("details")]
        public AgoraWebhookRecordingDetails? Details { get; init; }
    }

    /// <summary>
    /// Recording-specific details within an Agora webhook payload.
    /// Populated for recording_started and recording_stopped events.
    /// </summary>
    public sealed class AgoraWebhookRecordingDetails
    {
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; init; }

        [JsonPropertyName("sid")]
        public string? Sid { get; init; }

        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; init; }

        [JsonPropertyName("duration")]
        public int? Duration { get; init; }

        [JsonPropertyName("fileSize")]
        public long? FileSize { get; init; }
    }
}

