using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class RecordingFailedEvent : DomainEvent
    {
        public Guid SessionId { get; }

        public string Reason { get; }

        public RecordingFailedEvent(
            Guid sessionId,
            string reason)
        {
            SessionId = sessionId;
            Reason = reason;
        }
    }
}
