using System.Text.Json.Serialization;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Responses;

internal sealed record StartTaskResponse
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; init; } = string.Empty;

    [JsonPropertyName("create_ts")]
    public long? CreateTs { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
