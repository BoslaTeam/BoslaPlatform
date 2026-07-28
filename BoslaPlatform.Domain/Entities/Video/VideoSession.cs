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
        public string? AgoraRecordingUid { get; private set; }

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

            // A session may already be Ended (e.g. the Agora ChannelDestroyed webhook
            // fired when the last participant left) before the specialist's manual
            // finish or the expiration service runs. Ended is a natural precursor to
            // Completed — the session must still reach the Completed terminal state.
            // Both completion paths (SpecialistEnded and AppointmentExpired) converge
            // here so they produce an identical final state.
            bool alreadyEnded = Status == VideoSessionStatus.Ended;

            if (IsRecording)
                FailActiveRecording("Session completed while recording was active.");

            Status = VideoSessionStatus.Completed;
            EndedAt ??= DateTime.UtcNow;

            // The ended event was already raised when the session transitioned to
            // Ended; do not raise it again to avoid duplicate downstream handling.
            if (!alreadyEnded)
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
            string recordingSid,
            string? recordingUid = null)
        {
            AgoraRecordingId = recordingId;
            AgoraRecordingSid = recordingSid;
            if (recordingUid is not null)
            {
                AgoraRecordingUid = recordingUid;
            }
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

        /// <summary>
        /// Finalizes a recording whose S3 upload has been CONFIRMED (HeadObject
        /// succeeded). Unlike <see cref="StopRecording"/> this does not require the
        /// session to still be in the Recording state — it also promotes a recording
        /// parked in PendingUpload once a later completion path confirms the upload.
        /// Idempotent: a no-op once already Completed.
        /// </summary>
        public void CompleteRecordingVerified(
            string objectKey,
            long? fileSizeBytes,
            int? durationSeconds)
        {
            if (RecordingStatus is Domain.Enums.RecordingStatus.Completed)
                return;

            RecordingUrl = objectKey;
            RecordingStatus = Domain.Enums.RecordingStatus.Completed;
            RecordingCompletedAt ??= DateTime.UtcNow;
            CurrentRecording = null;
            CurrentRecordingId = null;

            AddDomainEvent(
                new RecordingCompletedEvent(Id, objectKey, durationSeconds, fileSizeBytes));
        }

        /// <summary>
        /// Provider Stop succeeded but the S3 upload has not been confirmed yet.
        /// The recording is neither Completed nor Failed — it is parked as
        /// PendingUpload so the async webhook / reconciliation can finalize it.
        /// Identifiers and CurrentRecording are intentionally retained. Does NOT set
        /// S3 metadata, so <see cref="IsUploadedToS3"/> stays false and the recording
        /// is not yet watchable.
        /// </summary>
        public void MarkRecordingUploadPending()
        {
            // Never downgrade a recording that a concurrent path already finalized.
            if (RecordingStatus is Domain.Enums.RecordingStatus.Completed)
                return;

            RecordingStatus = Domain.Enums.RecordingStatus.PendingUpload;
            CurrentRecording?.MarkPendingUpload();
        }

        /// <summary>
        /// Agora produced no file — nothing was captured to upload. Terminal failure.
        /// </summary>
        public void MarkRecordingUploadFailed(string? reason = null)
        {
            if (RecordingStatus is Domain.Enums.RecordingStatus.Completed)
                return;

            RecordingStatus = Domain.Enums.RecordingStatus.UploadFailed;
            RecordingFailureReason = reason;
            CurrentRecording?.MarkUploadFailed();

            AddDomainEvent(new RecordingFailedEvent(Id, reason ?? "Recording upload failed."));
        }

        /// <summary>
        /// The S3 object is missing/zero-length or verification errored persistently.
        /// The recording must NOT be treated as Completed.
        /// </summary>
        public void MarkRecordingVerificationFailed(string? reason = null)
        {
            if (RecordingStatus is Domain.Enums.RecordingStatus.Completed)
                return;

            RecordingStatus = Domain.Enums.RecordingStatus.VerificationFailed;
            RecordingFailureReason = reason;
            CurrentRecording?.MarkVerificationFailed();

            AddDomainEvent(new RecordingFailedEvent(Id, reason ?? "Recording S3 verification failed."));
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
            // Guard: channel_destroy only means "the session ended" if the session
            // was genuinely Active (ChannelCreated already fired for a real
            // participant). Agora emits channel_destroy for an empty-channel
            // condition on its OWN infrastructure state, independent of ours — a
            // client preview/connectivity-check connection, a stale or duplicate
            // webhook delivery, or out-of-order delivery relative to ChannelCreated
            // can all fire it while the session is still Waiting (nobody has
            // actually joined yet, possibly for an appointment that hasn't even
            // started). Treating that as Ended permanently blocks the real join
            // later, since JoinAsync rejects Ended sessions before it even checks
            // the appointment's time window. Only Active -> Ended is a genuine end;
            // Waiting -> Ended, Ended -> Ended, and Completed -> Ended are all
            // idempotent no-ops, mirroring the symmetric guard on ChannelCreated
            // (which only fires Waiting -> Active).
            if (Status != VideoSessionStatus.Active)
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
