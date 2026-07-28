namespace BoslaPlatform.Application.Observability
{
    /// <summary>
    /// The identity of one recording as it moves through the pipeline.
    ///
    /// WHY A DEDICATED CORRELATION ID:
    ///   A recording spans several independent HTTP requests — the specialist's
    ///   start call, Agora's webhooks, the stop call, and a later playback
    ///   request — plus background completion. A per-request correlation id
    ///   cannot join those. <see cref="CorrelationId"/> is derived from the
    ///   recording's own identity so every stage lands under the same key.
    /// </summary>
    public sealed record RecordingLogContext
    {
        public Guid? SessionId { get; init; }
        public Guid? AppointmentId { get; init; }
        public Guid? RecordingId { get; init; }
        public string? ChannelName { get; init; }
        public string? ResourceId { get; init; }
        public string? Sid { get; init; }
        public string? RecordingUid { get; init; }

        /// <summary>
        /// The provider that owns this recording ("Agora", …). Recorded on every
        /// stage so the timeline stays meaningful if the provider changes.
        /// </summary>
        public string Provider { get; init; } = "Agora";

        /// <summary>
        /// Retry attempt number for the stage (1-based). Lets the timeline show
        /// multiple attempts at the same stage without collapsing them.
        /// </summary>
        public int Attempt { get; init; } = 1;

        /// <summary>
        /// The canonical, vendor-neutral correlation id, when known. Derived from
        /// our own <see cref="RecordingId"/> — deliberately NOT the Agora SID, so a
        /// recording keeps one stable identity across retries, provider swaps, and
        /// the SID-less early stages. Set it explicitly to carry the id into stages
        /// (webhook, playback) that resolve the recording indirectly.
        /// </summary>
        public string? RecordingCorrelationId { get; init; }

        /// <summary>
        /// Stable identity for one recording across every stage and request.
        /// Order of preference: an explicit vendor-neutral id, then our recording
        /// id, then session/channel fallbacks for the SID-less early stages.
        /// The Agora SID is intentionally never the canonical id — it is only a
        /// join key the timeline store uses to stitch provider-emitted stages
        /// (which never see our recording id) back onto the recording.
        /// </summary>
        public string CorrelationId =>
            !string.IsNullOrWhiteSpace(RecordingCorrelationId) ? RecordingCorrelationId!
            : RecordingId is not null ? ForRecording(RecordingId.Value)
            : SessionId is not null ? $"sess-{SessionId}"
            : !string.IsNullOrWhiteSpace(Sid) ? $"sid-{Sid}"
            : !string.IsNullOrWhiteSpace(ChannelName) ? $"chan-{ChannelName}"
            : "rec-unknown";

        /// <summary>The canonical correlation id for a recording aggregate id.</summary>
        public static string ForRecording(Guid recordingId) => $"rec-{recordingId}";

        public static RecordingLogContext ForChannel(string channelName) =>
            new() { ChannelName = channelName };
    }
}
