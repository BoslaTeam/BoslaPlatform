using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class VideoSessionStartedEvent : DomainEvent
    {
        public Guid SessionId { get; }

        public DateTime StartedAtUtc { get; }

        public VideoSessionStartedEvent(Guid sessionId, DateTime startedAtUtc)
        {
            SessionId = sessionId;
            StartedAtUtc = startedAtUtc;
        }
    }
}
