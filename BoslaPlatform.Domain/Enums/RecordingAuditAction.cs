using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums
{
    /// <summary>
    /// Actions that are recorded in the recording audit log.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecordingAuditAction
    {
        /// <summary>A user viewed (watched) a recording via a presigned URL.</summary>
        Viewed,

        /// <summary>A user downloaded a recording.</summary>
        Downloaded,

        /// <summary>A recording was successfully uploaded to object storage.</summary>
        UploadCompleted,

        /// <summary>A recording upload failed after all retry attempts were exhausted.</summary>
        UploadFailed,

        /// <summary>A recording was deleted (soft or hard). Reserved for future use.</summary>
        Deleted
    }
}
