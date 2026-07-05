using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    /// <summary>
    /// Root request DTO for Agora webhook notifications.
    /// Represents the top-level payload sent by the Agora Notifications service.
    /// 
    /// WHY IT EXISTS:
    ///   Agora webhook callbacks send structured JSON payloads. This DTO maps the
    ///   top-level properties (noticeId, productId, eventType) and nested payload section.
    ///   
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Application layer (under Features/VideoSessions/Requests) as a DTO.
    ///   It is used by the controller to bind incoming HTTP requests and passed to the
    ///   application service for processing.
    ///
    /// HOW IT COMMUNICATES WITH THE DOMAIN:
    ///   Carries information from the outer infrastructure (Agora) into the application layer,
    ///   which then orchestrates updates on the Domain aggregate.
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
}
