using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    /// <summary>
    /// Domain event raised when an Agora cloud recording completes for a video session.
    /// </summary>
    public sealed class RecordingCompletedEvent : DomainEvent
    {
        /// <summary>
        /// The unique identifier of the video session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// The URL where the recording file is stored.
        /// </summary>
        public string RecordingUrl { get; }

        /// <summary>
        /// The duration of the recording in seconds.
        /// </summary>
        public int? DurationSeconds { get; }

        /// <summary>
        /// The file size of the recording in bytes.
        /// </summary>
        public long? FileSizeBytes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingCompletedEvent"/> class.
        /// </summary>
        /// <param name="sessionId">The video session identifier.</param>
        /// <param name="recordingUrl">The recording file URL.</param>
        /// <param name="durationSeconds">The recording duration in seconds.</param>
        /// <param name="fileSizeBytes">The recording file size in bytes.</param>
        public RecordingCompletedEvent(
            Guid sessionId,
            string recordingUrl,
            int? durationSeconds,
            long? fileSizeBytes)
        {
            SessionId = sessionId;
            RecordingUrl = recordingUrl;
            DurationSeconds = durationSeconds;
            FileSizeBytes = fileSizeBytes;
        }
    }
}
