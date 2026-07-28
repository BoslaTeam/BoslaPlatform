using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Domain.Models.Video
{
    public class ScreenRecording : AuditableEntity
    {
        private ScreenRecording()
        {
        }

        public string Url { get; private set; } = string.Empty;
        public RecordingStatus Status { get; private set; }
        public long? FileSizeBytes { get; private set; }
        public int? DurationSeconds { get; private set; }
        public RecordingAccessControl AccessControl { get; private set; }
        public RecordingStorageProvider StorageProvider { get; private set; }
        public string? AgoraRecordingId { get; private set; }
        public string? AgoraRecordingSid { get; private set; }
        public string? AgoraRecordingUid { get; private set; }
        public Guid VideoSessionId { get; private set; }
        public VideoSession? VideoSession { get; private set; }

        public static Result<ScreenRecording> Create(
            Guid videoSessionId,
            RecordingAccessControl accessControl,
            RecordingStorageProvider storageProvider)
        {
            return new ScreenRecording
            {
                VideoSessionId = videoSessionId,
                Status = RecordingStatus.Pending,
                AccessControl = accessControl,
                StorageProvider = storageProvider
            };
        }

        public void Complete(
            string url,
            long? fileSizeBytes,
            int? durationSeconds)
        {
            Url = url;
            FileSizeBytes = fileSizeBytes;
            DurationSeconds = durationSeconds;
            Status = RecordingStatus.Completed;
        }

        public void Fail()
        {
            Status = RecordingStatus.Failed;
        }

        /// <summary>
        /// Stop succeeded but S3 upload is not yet confirmed. The recording is not
        /// Completed and not Failed — an async path (webhook/reconcile) will finalize it.
        /// </summary>
        public void MarkPendingUpload()
        {
            Status = RecordingStatus.PendingUpload;
        }

        /// <summary>Agora captured/uploaded nothing — there is no file to verify.</summary>
        public void MarkUploadFailed()
        {
            Status = RecordingStatus.UploadFailed;
        }

        /// <summary>The S3 object is missing/zero-length or verification errored.</summary>
        public void MarkVerificationFailed()
        {
            Status = RecordingStatus.VerificationFailed;
        }

        public void AttachAgoraRecording(
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
    }
}
