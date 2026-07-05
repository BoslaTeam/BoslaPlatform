using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos
{
    public sealed class RecordingStartedEvent : DomainEvent
    {
        public Guid SessionId { get; }

        public Guid RecordingId { get; }

        public DateTime StartedAtUtc { get; }

        public string AgoraRecordingId { get; }

        public string AgoraRecordingSid { get; }

        public RecordingStartedEvent(
            Guid sessionId,
            Guid recordingId,
            DateTime startedAtUtc)
        {
            SessionId = sessionId;
            RecordingId = recordingId;
            StartedAtUtc = startedAtUtc;
            AgoraRecordingId = string.Empty;
            AgoraRecordingSid = string.Empty;
        }

        public RecordingStartedEvent(
            Guid sessionId,
            string agoraRecordingId,
            string agoraRecordingSid)
        {
            SessionId = sessionId;
            AgoraRecordingId = agoraRecordingId;
            AgoraRecordingSid = agoraRecordingSid;
            RecordingId = Guid.Empty;
            StartedAtUtc = DateTime.MinValue;
        }
    }
}
