using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums
{
    /// <summary>
    /// Classifies the category of a recording upload failure.
    /// Only <see cref="Transient"/> and <see cref="Network"/> failures are candidates for retry.
    /// <see cref="Permanent"/> and <see cref="Authentication"/> failures are not retried.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecordingFailureCategory
    {
        /// <summary>
        /// Temporary failure (e.g. 429, 503, timeout). Safe to retry.
        /// </summary>
        Transient,

        /// <summary>
        /// Permanent failure (e.g. 400 bad request, object key invalid). Do not retry.
        /// </summary>
        Permanent,

        /// <summary>
        /// Authentication / authorisation failure (e.g. 401, 403). Do not retry without fixing credentials.
        /// </summary>
        Authentication,

        /// <summary>
        /// Storage-layer failure (e.g. bucket misconfigured, quota exceeded). Operator intervention required.
        /// </summary>
        Storage,

        /// <summary>
        /// Network connectivity failure. Safe to retry.
        /// </summary>
        Network
    }
}
