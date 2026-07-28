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
        //
        // NOTE: these codes are small integers and overlap numerically with
        // nothing in the RTC set above (101-111), but they are only meaningful
        // when ProductId == ProductIds.CloudRecording. Always route on the pair.
        // ----------------------------------------------------------------

        /// <summary>
        /// eventType 40 ("recorder_started") — the recording service has started
        /// and joined the channel. Payload.details contains: resourceId, sid.
        /// </summary>
        public const int RecordingStarted = 40;

        /// <summary>
        /// eventType 41 ("recorder_leave") — the recorder left the channel.
        /// Carries a leaveCode explaining why (idle timeout, explicit stop, …).
        /// </summary>
        public const int RecorderLeave = 41;

        /// <summary>
        /// eventType 11 ("session_exit") — the cloud recording session ended and
        /// the service exited. This is the reliable "recording is over" signal.
        /// </summary>
        public const int SessionExit = 11;

        /// <summary>
        /// eventType 31 ("uploaded") — every recorded file reached the configured
        /// third-party storage. Payload.details carries the final file list.
        /// This is the authoritative "the object exists in S3" signal.
        /// </summary>
        public const int RecordingUploaded = 31;

        /// <summary>
        /// eventType 32 ("backuped") — files were uploaded, but at least one went
        /// to Agora Cloud Backup instead of our bucket (upload to S3 struggled).
        /// </summary>
        public const int RecordingBackedUp = 32;

        /// <summary>
        /// eventType 4 — the M3U8 playlist was generated and uploaded. Sent the
        /// first time the playlist appears, i.e. media is genuinely being captured.
        /// </summary>
        public const int PlaylistGenerated = 4;
    }

    /// <summary>
    /// Agora notification "productId" values. The same eventType number means
    /// different things per product, so routing must consider both.
    /// </summary>
    public static class ProductIds
    {
        public const int Rtc = 1;
        public const int CloudRecording = 3;
    }
}
