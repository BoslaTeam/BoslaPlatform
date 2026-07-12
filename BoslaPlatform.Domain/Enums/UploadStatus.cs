using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UploadStatus
    {
        Pending,
        Uploading,
        Uploaded,
        Failed,
        Retrying,
        Cancelled
    }
}