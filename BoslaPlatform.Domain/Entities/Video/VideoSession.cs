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
        public DateTime? RecordingCompletedAt { get; private set; }
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

            // Start() is a preparation/validation step only.
            // The session transitions to Active exclusively via ChannelCreated()
            // when Agora confirms the first participant has joined the channel.
            // No status change, no StartedAt, no VideoSessionStartedEvent raised here.

            return Result.Success();
        }

        public Result End()
        {
            // Idempotent: already ended.
            if (Status == VideoSessionStatus.Ended)
            {
                return Result.Success();
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

            AddDomainEvent(
                new ParticipantLeftVideoSessionEvent(
                    Id,
                    participant.UserId,
                    participant.AgoraUid));

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

        // ----------------------------------------------------------------
        // Webhook-facing aggregate methods
        // These are called exclusively by VideoSessionWebhookService.
        // They are the ONLY way Agora participant/recording/channel state
        // enters the domain — the frontend never calls these directly.
        //
        // ChannelCreated() is the SOLE activator for this aggregate.
        // Start() only validates and prepares — it does NOT set Status=Active.
        // ChannelDestroyed() handles Agora-triggered end.
        // End() handles specialist-triggered manual end.
        // ----------------------------------------------------------------

        /// <summary>
        /// Called when Agora fires eventType 103 (user_joined).
        /// Looks up the participant by AgoraUid (created at token-generation time).
        /// If no record exists for this AgoraUid, creates a sentinel entry so the
        /// domain event can still propagate.
        /// Raises <see cref="ParticipantJoinedVideoSessionEvent"/>.
        /// </summary>
        /// <param name="agoraUid">The Agora UID reported by the webhook.</param>
        /// <param name="resolvedUserId">
        ///   The platform UserId resolved from the participant record.
        ///   Pass <see cref="Guid.Empty"/> if no participant record was found.
        /// </param>
        public Result ParticipantJoined(
            long agoraUid,
            Guid resolvedUserId)
        {
            // Idempotent: duplicate webhook delivery is not an error.
            if (Status == VideoSessionStatus.Ended)
            {
                return Result.Success();
            }

            // If a participant record already exists for this AgoraUid,
            // this is a duplicate webhook delivery. Silently ignore.
            var existing = _participants
                .FirstOrDefault(x => x.AgoraUid == agoraUid);

            if (existing is not null)
            {
                return Result.Success();
            }

            if (resolvedUserId != Guid.Empty)
            {
                // Participant joined via a path that did not pre-create the record.
                // Create the record now as a Participant role (conservative default).
                var newParticipant = VideoSessionParticipant.Create(
                    Id,
                    resolvedUserId,
                    agoraUid,
                    VideoParticipantRole.Participant);

                _participants.Add(newParticipant);
            }

            AddDomainEvent(
                new ParticipantJoinedVideoSessionEvent(
                    Id,
                    resolvedUserId,
                    agoraUid));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 104 (user_left).
        /// Looks up the participant by AgoraUid and marks them as left.
        /// Raises <see cref="ParticipantLeftVideoSessionEvent"/>.
        /// If no participant record exists for the AgoraUid, the event is still
        /// raised with Guid.Empty as the UserId to ensure downstream handlers are notified.
        /// </summary>
        /// <param name="agoraUid">The Agora UID reported by the webhook.</param>
        public Result ParticipantLeft(long agoraUid)
        {
            var participant = _participants
                .FirstOrDefault(x => x.AgoraUid == agoraUid);

            // Idempotent: if no participant record exists for this AgoraUid,
            // this is a duplicate delivery or an unknown participant. Silently ignore.
            if (participant is null)
            {
                return Result.Success();
            }

            // Idempotent: if the participant already left, skip.
            if (participant.LeftAt is not null)
            {
                return Result.Success();
            }

            participant.MarkLeft();

            AddDomainEvent(
                new ParticipantLeftVideoSessionEvent(
                    Id,
                    participant.UserId,
                    agoraUid));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 110 (channel_create).
        /// The channel is created when the FIRST participant joins.
        ///
        /// This is the SOLE activator for the VideoSession aggregate.
        /// Only this method transitions the session from Waiting → Active.
        /// Start() is a preparation/validation step only and does NOT activate.
        ///
        /// Raises <see cref="VideoSessionStartedEvent"/> (business event: session is live)
        /// and <see cref="ChannelCreatedEvent"/> (Agora-specific integration event with
        /// channel name and Agora timestamp for monitoring/analytics).
        /// </summary>
        /// <param name="channelName">The Agora channel name from the webhook payload.</param>
        /// <param name="occurredAtUtc">The event timestamp from the Agora webhook.</param>
        public Result ChannelCreated(
            string channelName,
            DateTimeOffset occurredAtUtc)
        {
            // Idempotent: if the session is already Active or Ended,
            // this is a duplicate webhook delivery. Silently return success
            // without modifying state or raising any events.
            if (Status != VideoSessionStatus.Waiting)
            {
                return Result.Success();
            }

            // Status == Waiting: transition to Active.
            // This is the ONLY place where a session transitions Waiting → Active.
            // The Agora webhook is the authoritative source for "first participant joined."
            Status = VideoSessionStatus.Active;
            StartedAt = occurredAtUtc.UtcDateTime;

            AddDomainEvent(new VideoSessionStartedEvent(Id));
            AddDomainEvent(
                new ChannelCreatedEvent(Id, channelName, occurredAtUtc));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 111 (channel_destroy).
        /// The channel is destroyed when the LAST participant leaves.
        /// Transitions the session from any active state → Ended.
        ///
        /// Raises <see cref="VideoSessionEndedEvent"/> (business event: session ended)
        /// and <see cref="ChannelDestroyedEvent"/> (Agora-specific integration event
        /// with channel name and Agora timestamp for monitoring/analytics).
        /// </summary>
        /// <param name="channelName">The Agora channel name from the webhook payload.</param>
        /// <param name="occurredAtUtc">The event timestamp from the Agora webhook.</param>
        public Result ChannelDestroyed(
            string channelName,
            DateTimeOffset occurredAtUtc)
        {
            // Idempotent: if already Ended, this is a duplicate webhook delivery.
            if (Status == VideoSessionStatus.Ended)
            {
                return Result.Success();
            }

            Status = VideoSessionStatus.Ended;
            EndedAt = occurredAtUtc.UtcDateTime;

            AddDomainEvent(new VideoSessionEndedEvent(Id, AppointmentId));
            AddDomainEvent(
                new ChannelDestroyedEvent(Id, AppointmentId, channelName, occurredAtUtc));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 1001 (cloud_recording_started).
        /// Records the Agora recording identifiers on the session aggregate.
        /// Raises <see cref="RecordingStartedEvent"/>.
        /// </summary>
        /// <param name="resourceId">The Agora recording resource identifier.</param>
        /// <param name="sid">The Agora recording session identifier (SID).</param>
        public Result RecordingStarted(
            string resourceId,
            string sid)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return Error.Validation(
                    "VideoSession.Recording.ResourceIdRequired",
                    "Agora recording resourceId is required.");
            }

            // Idempotent: recording already started for this session.
            if (AgoraRecordingId is not null)
            {
                return Result.Success();
            }

            AgoraRecordingId = resourceId;
            AgoraRecordingSid = sid;

            AddDomainEvent(
                new RecordingStartedEvent(Id, resourceId, sid));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 1003 (cloud_recording_stopped).
        /// Updates the recording URL on the session and raises
        /// <see cref="RecordingCompletedEvent"/>.
        /// </summary>
        /// <param name="fileUrl">The URL of the completed recording file (may be null if not yet uploaded).</param>
        /// <param name="durationSeconds">Recording duration in seconds.</param>
        /// <param name="fileSizeBytes">Recording file size in bytes.</param>
        public Result RecordingStopped(
            string? fileUrl,
            int? durationSeconds,
            long? fileSizeBytes)
        {
            // Idempotent: recording completion already processed.
            if (RecordingCompletedAt is not null)
            {
                return Result.Success();
            }

            if (fileUrl is not null)
            {
                RecordingUrl = fileUrl;
            }

            RecordingCompletedAt = DateTime.UtcNow;

            AddDomainEvent(
                new RecordingCompletedEvent(
                    Id,
                    fileUrl ?? string.Empty,
                    durationSeconds,
                    fileSizeBytes));

            return Result.Success();
        }
    }
}
