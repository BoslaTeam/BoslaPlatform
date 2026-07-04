using System.Text.Json.Serialization;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Requests;

internal sealed record StartTaskRequest
{
    [JsonPropertyName("languages")]
    public string[] Languages { get; init; } = [];

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("maxIdleTime")]
    public int MaxIdleTime { get; init; } = 60;

    [JsonPropertyName("rtcConfig")]
    public RtcConfig RtcConfig { get; init; } = new();
}

internal sealed record RtcConfig
{
    [JsonPropertyName("channelName")]
    public string ChannelName { get; init; } = string.Empty;

    [JsonPropertyName("subBotUid")]
    public string SubBotUid { get; init; } = string.Empty;

    [JsonPropertyName("pubBotUid")]
    public string PubBotUid { get; init; } = string.Empty;

    [JsonPropertyName("subscribeAudioUids")]
    public string[] SubscribeAudioUids { get; init; } = ["all"];
}
