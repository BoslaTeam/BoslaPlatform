using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    /// <summary>
    /// Root request DTO for Agora webhook notifications.
    /// Represents the top-level payload sent by the Agora Notifications service.
    /// </summary>
    public sealed class AgoraWebhookRequest
    {
        /// <summary>
        /// The unique notification identifier assigned by Agora.
        /// </summary>
        [JsonPropertyName("noticeId")]
        public string NoticeId { get; set; } = string.Empty;

        /// <summary>
        /// The Agora product type that generated the event (e.g., 1 = Rtc, 3 = Cloud Recording).
        /// </summary>
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        /// <summary>
        /// The numeric event type code from Agora.
        /// </summary>
        [JsonPropertyName("eventType")]
        public int EventType { get; set; }

        /// <summary>
        /// The event payload containing channel and user details.
        /// </summary>
        [JsonPropertyName("payload")]
        public AgoraWebhookPayload Payload { get; set; } = new();
    }

    /// <summary>
    /// Payload section of an Agora webhook notification.
    /// Contains channel information and event-specific details.
    /// </summary>
    public sealed class AgoraWebhookPayload
    {
        /// <summary>
        /// The Agora channel name associated with the event.
        /// </summary>
        [JsonPropertyName("channelName")]
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// The Agora UID of the user involved in the event (for user_joined/user_left).
        /// </summary>
        [JsonPropertyName("uid")]
        public long Uid { get; set; }

        /// <summary>
        /// The Agora recording resource ID (for recording events).
        /// </summary>
        [JsonPropertyName("sid")]
        public string? Sid { get; set; }

        /// <summary>
        /// The sequence number of the event.
        /// </summary>
        [JsonPropertyName("sequence")]
        public int Sequence { get; set; }

        /// <summary>
        /// The timestamp when the event occurred (Unix epoch in milliseconds).
        /// </summary>
        [JsonPropertyName("ts")]
        public long Ts { get; set; }

        /// <summary>
        /// The event-specific details for recording operations.
        /// </summary>
        [JsonPropertyName("details")]
        public AgoraWebhookRecordingDetails? Details { get; set; }
    }

    /// <summary>
    /// Recording-specific details within an Agora webhook payload.
    /// Populated for recording_started and recording_stopped events.
    /// </summary>
    public sealed class AgoraWebhookRecordingDetails
    {
        /// <summary>
        /// The Agora recording resource identifier.
        /// </summary>
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }

        /// <summary>
        /// The Agora recording session identifier (SID).
        /// </summary>
        [JsonPropertyName("sid")]
        public string? Sid { get; set; }

        /// <summary>
        /// The URL of the completed recording file.
        /// </summary>
        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; set; }

        /// <summary>
        /// The duration of the recording in seconds.
        /// </summary>
        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        /// <summary>
        /// The file size of the recording in bytes.
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }
    }
}
