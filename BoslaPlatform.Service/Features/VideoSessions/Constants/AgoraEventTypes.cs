namespace BoslaPlatform.Application.Features.VideoSessions.Constants
{
    /// <summary>
    /// Agora webhook event type codes.
    ///
    /// WHY IT EXISTS:
    ///   Agora sends numeric event type codes in the webhook payload. Scattering magic
    ///   numbers across the codebase would make the system fragile and unreadable.
    ///   This constants class provides a single authoritative source of truth for
    ///   every event type code the platform cares about.
    ///
    /// SOURCE:
    ///   https://docs.agora.io/en/video-calling/reference/agora-notification-service/
    ///   — Table: "Event types for channel and user callbacks"
    ///   — Table: "Event types for cloud recording callbacks"
    ///
    /// CLEAN ARCHITECTURE PLACEMENT:
    ///   Application layer. These constants are used by VideoSessionWebhookService
    ///   (Application) to route events, and referenced by the controller
    ///   (API) only indirectly through the service. The domain aggregate methods
    ///   are unaware of these codes — they receive already-interpreted calls.
    /// </summary>
    public static class AgoraEventTypes
    {
        // ----------------------------------------------------------------
        // RTC Channel / User Events  (productId = 1)
        // ----------------------------------------------------------------

        /// <summary>
        /// eventType 103 — A user joined the channel.
        /// Payload contains: channelName, uid, ts.
        /// </summary>
        public const int UserJoined = 103;

        /// <summary>
        /// eventType 104 — A user left the channel.
        /// Payload contains: channelName, uid, ts.
        /// </summary>
        public const int UserLeft = 104;

        /// <summary>
        /// eventType 110 — A channel was created (first user joined).
        /// Payload contains: channelName, ts.
        /// </summary>
        public const int ChannelCreated = 110;

        /// <summary>
        /// eventType 111 — A channel was destroyed (last user left).
        /// Payload contains: channelName, ts.
        /// </summary>
        public const int ChannelDestroyed = 111;

        // ----------------------------------------------------------------
        // Cloud Recording Events  (productId = 3)
        // ----------------------------------------------------------------

        /// <summary>
        /// eventType 1001 — Cloud recording has started.
        /// Payload.details contains: resourceId, sid.
        /// </summary>
        public const int RecordingStarted = 1001;

        /// <summary>
        /// eventType 1003 — Cloud recording has stopped.
        /// Payload.details contains: resourceId, sid, fileUrl, duration, fileSize.
        /// This is sent when recording stops either by explicit API call or countdown.
        /// </summary>
        public const int RecordingStopped = 1003;

        /// <summary>
        /// eventType 1004 — Cloud recording has been uploaded to the cloud vendor.
        /// We handle this as RecordingStopped for simplicity — the file is available.
        /// </summary>
        public const int RecordingUploaded = 1004;
    }
}
