using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    /// <summary>
    /// Domain event raised when a participant joins a video session via Agora webhook.
    /// </summary>
    public sealed class ParticipantJoinedVideoSessionEvent : DomainEvent
    {
        /// <summary>
        /// The unique identifier of the video session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// The unique identifier of the participant.
        /// </summary>
        public Guid ParticipantId { get; }

        /// <summary>
        /// The Agora UID of the participant.
        /// </summary>
        public long AgoraUid { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantJoinedVideoSessionEvent"/> class.
        /// </summary>
        /// <param name="sessionId">The video session identifier.</param>
        /// <param name="participantId">The participant identifier.</param>
        /// <param name="agoraUid">The Agora UID of the participant.</param>
        public ParticipantJoinedVideoSessionEvent(
            Guid sessionId,
            Guid participantId,
            long agoraUid)
        {
            SessionId = sessionId;
            ParticipantId = participantId;
            AgoraUid = agoraUid;
        }
    }
}
