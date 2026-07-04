namespace BoslaPlatform.Shared.Constants
{
    public static class VideoSignalREvents
    {
        public const string SessionStarted = nameof(SessionStarted);

        public const string SessionEnded = nameof(SessionEnded);

        public const string ParticipantJoined = nameof(ParticipantJoined);

        public const string ParticipantLeft = nameof(ParticipantLeft);

        public const string RecordingStarted = nameof(RecordingStarted);

        public const string RecordingStopped = nameof(RecordingStopped);

        /// <summary>
        /// Fired when a live transcript segment is updated.
        /// The same segment (identified by SequenceNumber) may fire multiple times:
        /// Partial → Partial → Final.
        /// Clients should replace the caption for that SequenceNumber rather than append.
        /// </summary>
        public const string TranscriptUpdated = nameof(TranscriptUpdated);
    }
}
