using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class VideoSessionStartedEvent : DomainEvent
    {
        public Guid SessionId { get; }

        public VideoSessionStartedEvent(Guid sessionId)
        {
            SessionId = sessionId;
        }
    }
}
