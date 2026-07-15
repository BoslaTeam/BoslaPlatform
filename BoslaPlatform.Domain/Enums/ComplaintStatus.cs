using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComplaintStatus
{
    Pending,
    Reviewed,
    ResolvedRefunded,
    ResolvedRejected
}
