using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    public sealed class AgoraWebhookEvent
    {
        [JsonPropertyName("noticeId")]
        public string NoticeId { get; init; } = string.Empty;

        [JsonPropertyName("productId")]
        public int ProductId { get; init; }

        [JsonPropertyName("eventType")]
        public int EventType { get; init; }

        [JsonPropertyName("notifyMs")]
        public long NotifyMs { get; init; }

        [JsonPropertyName("payload")]
        public AgoraWebhookPayload Payload { get; init; } = new();
    }
}
