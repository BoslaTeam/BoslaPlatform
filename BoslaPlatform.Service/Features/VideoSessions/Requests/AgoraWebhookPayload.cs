using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public sealed class AgoraWebhookPayload
    {
        [JsonPropertyName("channelName")]
        public string ChannelName { get; init; } = string.Empty;

        [JsonPropertyName("uid")]
        public string Uid { get; init; } = string.Empty;

        [JsonPropertyName("ts")]
        public long Ts { get; init; }

        [JsonPropertyName("sid")]
        public string Sid { get; init; } = string.Empty;
    }
}
