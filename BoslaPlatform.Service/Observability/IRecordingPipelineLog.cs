namespace BoslaPlatform.Application.Observability
{
    /// <summary>
    /// Emits one canonical structured event per recording pipeline stage.
    ///
    /// Every stage writes the same field set (CorrelationId, SessionId,
    /// AppointmentId, RecordingId, ResourceId, Sid, Stage, Outcome, DurationMs,
    /// TimestampUtc), so a single query on CorrelationId or SessionId
    /// reconstructs the whole lifecycle and shows exactly where it stopped.
    /// </summary>
    public interface IRecordingPipelineLog
    {
        /// <summary>Records that a stage was entered.</summary>
        void Started(RecordingStage stage, RecordingLogContext context);

        /// <summary>Records that a stage completed successfully.</summary>
        void Succeeded(
            RecordingStage stage,
            RecordingLogContext context,
            TimeSpan? duration = null,
            IReadOnlyDictionary<string, object?>? extra = null);

        /// <summary>
        /// Records that a stage failed. Always logged at Error — a broken stage
        /// must never be discoverable only by its absence.
        /// </summary>
        void Failed(
            RecordingStage stage,
            RecordingLogContext context,
            string errorCode,
            string errorDescription,
            Exception? exception = null,
            TimeSpan? duration = null);
    }
}
