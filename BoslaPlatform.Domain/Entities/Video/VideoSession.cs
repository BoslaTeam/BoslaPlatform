using System.ComponentModel.DataAnnotations;
using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Domain.Models.Video
{
    public class VideoSession : AuditableEntity
    {
        public Guid AppointmentId { get; private set; }
        public VideoSessionType Type { get; private set; }
        public string AgoraChannelName { get; private set; } = string.Empty;
        public string AgoraAppId { get; private set; } = string.Empty;
        public VideoSessionStatus Status { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        // Recording state (aggregate root owns current recording lifecycle)
        public RecordingStatus? RecordingStatus { get; private set; }
        public DateTime? RecordingStartedAtUtc { get; private set; }
        public DateTime? RecordingCompletedAt { get; private set; }
        public string? RecordingFailureReason { get; private set; }
        public Guid? CurrentRecordingId { get; private set; }
        public ScreenRecording? CurrentRecording { get; private set; }

        // Provider-level recording identifiers (Agora Cloud Recording)
        public string? AgoraRecordingId { get; private set; }
        public string? AgoraRecordingSid { get; private set; }

        // Recording file URL from Agora provider (S3 object path)
        public string? RecordingUrl { get; private set; }

        // Amazon S3 recording metadata (populated after Agora uploads to S3)
        public StorageProvider? StorageProvider { get; private set; }
        public string? BucketName { get; private set; }
        public string? ObjectKey { get; private set; }
        public string? ContentType { get; private set; }
        public long? ContentLength { get; private set; }
        public int? DurationSeconds { get; private set; }
        public string? ETag { get; private set; }
        public DateTime? S3UploadedAtUtc { get; private set; }

        // Retention policy (architecture stub — deletion not yet implemented)
        public DateTime? ExpiresAtUtc { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }

        // Optimistic concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; private set; }

        public Appointment? Appointment { get; private set; }
        private readonly List<ScreenRecording> _recordings = [];

        public IReadOnlyCollection<ScreenRecording> Recordings
            => _recordings.AsReadOnly();
        public IReadOnlyCollection<VideoSessionParticipant> Participants
            => _participants.AsReadOnly();

        public bool IsRecording => RecordingStatus is Domain.Enums.RecordingStatus.Recording;

        public bool IsUploadedToS3
            => !string.IsNullOrWhiteSpace(ObjectKey)
                && !string.IsNullOrWhiteSpace(BucketName);

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

            AddDomainEvent(new VideoSessionStartedEvent(Id, StartedAt.Value));

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

            AddDomainEvent(new VideoSessionEndedEvent(Id, AppointmentId, EndedAt.Value));

            return Result.Success();
        }

        public Result Complete()
        {
            if (Status == VideoSessionStatus.Completed)
                return Result.Success();

            if (Status == VideoSessionStatus.Ended)
                return Error.Validation(
                    "VideoSession.AlreadyEnded",
                    "Cannot complete an already ended session.");

            if (IsRecording)
                FailActiveRecording("Session completed while recording was active.");

            Status = VideoSessionStatus.Completed;
            EndedAt = DateTime.UtcNow;

            AddDomainEvent(new VideoSessionEndedEvent(Id, AppointmentId, EndedAt.Value));

            return Result.Success();
        }

        public Result ParticipantRejoined(Guid userId)
        {
            if (Status == VideoSessionStatus.Completed)
                return Error.Validation("VideoSession.Completed", "This session has been completed and cannot be rejoined.");

            var participant = _participants.FirstOrDefault(x => x.UserId == userId);

            if (participant is null)
                return Error.NotFound("VideoSessionParticipant.NotFound", "Participant not found.");

            if (participant.LeftAt is null)
                return Result.Success();

            participant.ClearLeaveState();

            AddDomainEvent(new ParticipantRejoinedVideoSessionEvent(Id, userId));

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
            string recordingSid)
        {
            AgoraRecordingId = recordingId;
            AgoraRecordingSid = recordingSid;
        }

        // ----------------------------------------------------------------
        // Command-facing recording methods
        // Called by VideoSessionService (Application layer) on user action.
        // Commands initiate state transitions.
        // ----------------------------------------------------------------


        public Result StartRecording(ScreenRecording recording)
        {
            if (recording.VideoSessionId != Id)
            {
                return Error.Validation(
                    "Recording.InvalidSession",
                    "Recording does not belong to this session.");
            }

            if (Status != VideoSessionStatus.Active)
            {
                return Error.Validation(
                    "VideoSession.NotActive",
                    "Session must be active.");
            }

            if (_recordings.Contains(recording))
            {
                return Error.Conflict(
                    "Recording.Exists",
                    "Recording already attached.");
            }

            if (IsRecording)
            {
                return Error.Conflict(
                    "VideoSession.AlreadyRecording",
                    "Recording already running.");
            }

            _recordings.Add(recording);

            CurrentRecording = recording;

            RecordingStatus = Enums.RecordingStatus.Recording;
            RecordingStartedAtUtc = DateTime.UtcNow;

            return Result.Success();
        }

        public Result SetCurrentRecording(ScreenRecording recording)
        {
            if (recording.VideoSessionId != Id)
            {
                return Error.Validation(
                    "Recording.InvalidSession",
                    "Recording does not belong to this session.");
            }

            if (recording.Id == Guid.Empty)
            {
                return Error.Validation(
                    "Recording.IdNotAssigned",
                    "Recording must be saved before it can become the current recording.");
            }

            if (!_recordings.Contains(recording))
            {
                return Error.Validation(
                    "Recording.NotAttached",
                    "Recording must be attached to this session before it can become current.");
            }

            CurrentRecording = recording;
            CurrentRecordingId = recording.Id;

            AddDomainEvent(
                new RecordingStartedEvent(
                    Id,
                    recording.Id,
                    RecordingStartedAtUtc ?? DateTime.UtcNow));

            return Result.Success();
        }

        public Result StopRecording()
        {
            if (!IsRecording)
            {
                return Error.Validation(
                    "VideoSession.NotRecording",
                    "No active recording.");
            }

            CurrentRecording = null;
            CurrentRecordingId = null;
            RecordingStatus = Enums.RecordingStatus.Completed;
            RecordingCompletedAt = DateTime.UtcNow;

            AddDomainEvent(
                new RecordingCompletedEvent(
                    Id,
                    string.Empty,
                    null,
                    null));

            return Result.Success();
        }

        public Result FailActiveRecording(string? reason = null)
        {
            if (!IsRecording)
            {
                return Error.Validation(
                    "VideoSession.NotRecording",
                    "No active recording to fail.");
            }

            CurrentRecording?.Fail();
            CurrentRecording = null;
            CurrentRecordingId = null;
            RecordingStatus = Enums.RecordingStatus.Failed;
            RecordingFailureReason = reason;

            AddDomainEvent(
                new RecordingFailedEvent(
                    Id,
                    reason ?? "Recording failed."));

            return Result.Success();
        }
        // ----------------------------------------------------------------
        // Webhook-facing recording methods
        // Called exclusively by VideoSessionWebhookService.
        // Webhooks confirm provider state — they never initiate business logic.
        // ----------------------------------------------------------------

        // ----------------------------------------------------------------
        // Recording upload lifecycle
        // Called after Agora Cloud Recording uploads to Amazon S3.
        // ----------------------------------------------------------------

        /// <summary>
        /// Persists the Amazon S3 metadata for a completed recording.
        /// Called after Agora confirms the recording file is available in S3.
        /// </summary>
        public void SetS3RecordingMetadata(
            StorageProvider storageProvider,
            string bucketName,
            string objectKey,
            string contentType,
            long contentLength,
            int? durationSeconds = null,
            string? etag = null)
        {
            StorageProvider = storageProvider;
            BucketName = bucketName;
            ObjectKey = objectKey;
            ContentType = contentType;
            ContentLength = contentLength;
            DurationSeconds = durationSeconds;
            ETag = etag;
            S3UploadedAtUtc = DateTime.UtcNow;
        }

        // ----------------------------------------------------------------
        // Retention policy stubs
        // Architecture prepared. Actual deletion not yet implemented.
        // ----------------------------------------------------------------

        /// <summary>
        /// Sets the expiry timestamp for future scheduled cleanup.
        /// Does NOT delete the recording — see future RetentionCleanupService.
        /// </summary>
        public void MarkExpired()
        {
            ExpiresAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Soft-deletes the recording by recording the deletion timestamp.
        /// The physical file in object storage is NOT removed by this call.
        /// </summary>
        public Result MarkSoftDeleted()
        {
            if (DeletedAtUtc.HasValue)
                return Error.Conflict("Recording.AlreadyDeleted", "Recording is already marked as deleted.");

            DeletedAtUtc = DateTime.UtcNow;
            return Result.Success();
        }

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

            AddDomainEvent(new VideoSessionStartedEvent(Id, StartedAt.Value));
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

            // Fail any active recording when channel is destroyed.
            // The recording cannot continue without the Agora channel.
            if (IsRecording)
            {
                FailActiveRecording("Channel destroyed while recording was active.");
            }

            AddDomainEvent(new VideoSessionEndedEvent(Id, AppointmentId, EndedAt.Value));
            AddDomainEvent(
                new ChannelDestroyedEvent(Id, AppointmentId, channelName, occurredAtUtc));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 1001 (cloud_recording_started).
        /// Confirms provider recording state. Idempotent — never initiates
        /// business logic if the command already transitioned the aggregate.
        /// </summary>
        public Result ConfirmRecordingStarted(
            string resourceId,
            string sid)
        {
            AgoraRecordingId = resourceId;
            AgoraRecordingSid = sid;

            if (RecordingStatus is not null)
            {
                return Result.Success();
            }

            RecordingStatus = Domain.Enums.RecordingStatus.Recording;
            RecordingStartedAtUtc = DateTime.UtcNow;

            AddDomainEvent(
                new RecordingStartedEvent(Id, resourceId, sid));

            return Result.Success();
        }

        /// <summary>
        /// Called when Agora fires eventType 1003 (cloud_recording_stopped).
        /// Confirms provider recording finalization. Idempotent — never
        /// re-raises events if the command already completed the lifecycle.
        /// </summary>
        public Result ConfirmRecordingStopped(
            string? fileUrl,
            int? durationSeconds,
            long? fileSizeBytes)
        {
            if (RecordingCompletedAt is not null
                || RecordingStatus == Domain.Enums.RecordingStatus.Failed)
            {
                return Result.Success();
            }

            var wasCommanded = RecordingStatus is not null
                && RecordingStatus != Domain.Enums.RecordingStatus.Completed;

            if (fileUrl is not null)
            {
                RecordingUrl = fileUrl;
            }

            RecordingCompletedAt = DateTime.UtcNow;
            RecordingStatus = Domain.Enums.RecordingStatus.Completed;

            if (wasCommanded)
            {
                return Result.Success();
            }

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
