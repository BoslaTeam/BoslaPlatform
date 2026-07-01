using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    /// <summary>
    /// Domain event raised when an Agora cloud recording starts for a video session.
    /// </summary>
    public sealed class RecordingStartedEvent : DomainEvent
    {
        /// <summary>
        /// The unique identifier of the video session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// The Agora recording identifier.
        /// </summary>
        public string AgoraRecordingId { get; }

        /// <summary>
        /// The Agora recording session identifier.
        /// </summary>
        public string AgoraRecordingSid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingStartedEvent"/> class.
        /// </summary>
        /// <param name="sessionId">The video session identifier.</param>
        /// <param name="agoraRecordingId">The Agora recording identifier.</param>
        /// <param name="agoraRecordingSid">The Agora recording session identifier.</param>
        public RecordingStartedEvent(
            Guid sessionId,
            string agoraRecordingId,
            string agoraRecordingSid)
        {
            SessionId = sessionId;
            AgoraRecordingId = agoraRecordingId;
            AgoraRecordingSid = agoraRecordingSid;
        }
    }
}
