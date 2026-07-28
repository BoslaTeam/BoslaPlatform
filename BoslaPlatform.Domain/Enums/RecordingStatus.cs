using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecordingStatus
    {
        Processing,
        Ready,
        Failed,
        Deleted,
        Pending,
        Completed,
        Recording,
        Starting,
        Stopping,
        Idle,
        Uploading,
        Uploaded,
        Cancelled,

        // Upload-verification states (Agora Stop returned, but S3 not yet confirmed).
        // Appended at the end: RecordingStatus is persisted as a string, so new
        // members need no data migration and no existing value shifts.

        /// <summary>Stop succeeded but the S3 object was not confirmed within the
        /// synchronous window; the async webhook / reconciliation will finalize it.</summary>
        PendingUpload,

        /// <summary>Agora produced no file at all — nothing was captured to upload.</summary>
        UploadFailed,

        /// <summary>The S3 object is missing or zero-length, or HeadObject errored
        /// persistently — the recording must NOT be treated as Completed.</summary>
        VerificationFailed
    }
}
