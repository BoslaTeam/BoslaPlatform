using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Domain.Models.Video
{
    public class VideoSession:AuditableEntity
    {
        public Guid AppointmentId { get; private set; }
        public VideoSessionType Type { get; private set; }
        public string AgoraChannelName { get; private set; } = string.Empty;
        public string AgoraAppId { get; private set; } = string.Empty;
        public VideoSessionStatus Status { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public string? AgoraRecordingId { get; private set; }
        public string? AgoraRecordingSid { get; private set; }
        public string? RecordingUrl { get; private set; }
        public Appointment? Appointment { get; private set; }

        public IReadOnlyCollection<VideoSessionParticipant> Participants
            => _participants.AsReadOnly();

        // Navigation
        private readonly List<VideoSessionParticipant> _participants = [];
        public static Result<VideoSession> Create(
        Guid appointmentId,
        string agoraChannelName,
        string agoraAppId,
        VideoSessionType type)
        {
            if (string.IsNullOrWhiteSpace(agoraChannelName))
            {
                return Error.Validation(
                    "VideoSession.ChannelRequired",
                    "Channel name is required.");
            }

            var session = new VideoSession
            {
                AppointmentId = appointmentId,
                AgoraChannelName = agoraChannelName,
                AgoraAppId = agoraAppId,
                Type = type,
                Status = VideoSessionStatus.Waiting
            };

            return session;
        }

        public Result Start()
        {
            if (Status == VideoSessionStatus.Active)
            {
                return Error.Validation(
                    "VideoSession.AlreadyStarted",
                    "Session already started.");
            }

            if (Status == VideoSessionStatus.Ended)
            {
                return Error.Validation(
                    "VideoSession.AlreadyEnded",
                    "Session already ended.");
            }

            Status = VideoSessionStatus.Active;
            StartedAt = DateTime.UtcNow;

            AddDomainEvent(
                new VideoSessionStartedEvent(Id));

            return Result.Success();
        }

        public Result End()
        {
            if (Status == VideoSessionStatus.Ended)
            {
                return Error.Validation(
                    "VideoSession.AlreadyEnded",
                    "Session already ended.");
            }

            Status = VideoSessionStatus.Ended;
            EndedAt = DateTime.UtcNow;

            AddDomainEvent(new VideoSessionEndedEvent(Id, AppointmentId));
               

            return Result.Success();
        }

        public Result AddParticipant(
            Guid userId,
            long agoraUid,
            VideoParticipantRole role)
        {
            if (Status == VideoSessionStatus.Ended)
            {
                return Error.Validation(
                    "VideoSession.Ended",
                    "Cannot join ended session.");
            }

            if (_participants.Any(x => x.UserId == userId))
            {
                return Error.Conflict(
                    "VideoSessionParticipant.Exists",
                    "Participant already joined.");
            }

            var participant =
                VideoSessionParticipant.Create(
                    Id,
                    userId,
                    agoraUid,
                    role);

            _participants.Add(participant);

            AddDomainEvent(
                new VideoSessionParticipantJoinedEvent(
                    Id,
                    userId));

            return Result.Success();
        }

        public Result MarkParticipantLeft(Guid userId)
        {
            var participant = _participants
                .FirstOrDefault(x => x.UserId == userId);

            if (participant is null)
            {
                return Error.NotFound(
                    "VideoSessionParticipant.NotFound",
                    "Participant not found.");
            }

            participant.MarkLeft();

            return Result.Success();
        }

        public void SetRecording(
            string recordingId,
            string recordingSid,
            string recordingUrl)
        {
            AgoraRecordingId = recordingId;
            AgoraRecordingSid = recordingSid;
            RecordingUrl = recordingUrl;
        }
    }
}
