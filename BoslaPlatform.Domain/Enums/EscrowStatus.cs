using System.Text.Json.Serialization;

namespace BoslaPlatform.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EscrowStatus
{
    Held,
    Released,
    Disputed,
    Refunded
}
