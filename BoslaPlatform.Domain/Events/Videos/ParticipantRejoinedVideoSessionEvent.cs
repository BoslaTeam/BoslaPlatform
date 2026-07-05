using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class ParticipantRejoinedVideoSessionEvent : DomainEvent
    {
        public Guid SessionId { get; }

        public Guid ParticipantId { get; }

        public ParticipantRejoinedVideoSessionEvent(Guid sessionId, Guid participantId)
        {
            SessionId = sessionId;
            ParticipantId = participantId;
        }
    }
}
