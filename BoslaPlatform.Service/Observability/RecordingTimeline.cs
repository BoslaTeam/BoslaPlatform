namespace BoslaPlatform.Application.Observability
{
    /// <summary>One persisted occurrence of a pipeline stage.</summary>
    public sealed record RecordingTimelineEntry
    {
        public required string Stage { get; init; }
        public required string Outcome { get; init; }   // Started | Succeeded | Failed
        public required DateTime OccurredAtUtc { get; init; }
        public string Provider { get; init; } = "Agora";
        public int Attempt { get; init; } = 1;
        public double? DurationMs { get; init; }
        public Guid? SessionId { get; init; }
        public Guid? RecordingId { get; init; }
        public string? ResourceId { get; init; }
        public string? Sid { get; init; }
        public string? ChannelName { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorDescription { get; init; }
        public string? Detail { get; init; }
    }

    /// <summary>
    /// A full recording lifecycle reconstructed for one correlation id, ordered
    /// in time, with a derived health verdict — everything the diagnostics
    /// endpoint needs without an operator ever opening a log file.
    /// </summary>
    public sealed record RecordingTimeline
    {
        public required string CorrelationId { get; init; }
        public IReadOnlyList<RecordingTimelineEntry> Entries { get; init; } = [];

        /// <summary>The furthest stage that reached a successful outcome.</summary>
        public string? FurthestStageReached { get; init; }

        /// <summary>The stage that failed, if the pipeline ended in failure.</summary>
        public string? FailedStage { get; init; }

        /// <summary>
        /// "Healthy" (reached MetadataSaved), "InProgress", or "Failed@{stage}".
        /// One field an operator can read to know where a recording stands.
        /// </summary>
        public required string Verdict { get; init; }
    }

    /// <summary>
    /// Append-only store of pipeline stage events, queryable by correlation id.
    /// This is what makes the timeline reconstructable without reading raw logs.
    /// </summary>
    public interface IRecordingTimelineStore
    {
        /// <summary>
        /// Reconstructs the timeline for a canonical correlation id. Stitches in
        /// provider-emitted stages (which only carry the SID/resourceId) using the
        /// supplied join keys, so a caller can pass the keys it resolved from the
        /// recording aggregate.
        /// </summary>
        Task<RecordingTimeline> GetTimelineAsync(
            string correlationId,
            RecordingTimelineJoinKeys joinKeys,
            CancellationToken ct = default);
    }

    /// <summary>
    /// The provider identifiers a recording accumulated, used to stitch stages
    /// emitted before the canonical correlation id was known.
    /// </summary>
    public sealed record RecordingTimelineJoinKeys
    {
        public Guid? RecordingId { get; init; }
        public Guid? SessionId { get; init; }
        public string? Sid { get; init; }
        public string? ResourceId { get; init; }
        public string? ChannelName { get; init; }
    }
}
