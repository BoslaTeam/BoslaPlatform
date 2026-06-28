using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class VideoSessionParticipantJoinedEvent
    : DomainEvent
    {
        public Guid SessionId { get; }
        public Guid UserId { get; }
        public VideoSessionParticipantJoinedEvent(
            Guid sessionId,
            Guid userId)
        {
            SessionId = sessionId;
            UserId = userId;
        }
    }
}
