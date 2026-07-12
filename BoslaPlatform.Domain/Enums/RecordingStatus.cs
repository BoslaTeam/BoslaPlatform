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
        Cancelled
    }
}
