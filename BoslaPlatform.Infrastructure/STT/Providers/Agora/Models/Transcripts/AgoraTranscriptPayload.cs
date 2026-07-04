using System.Text.Json.Serialization;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Transcripts;

internal sealed record AgoraTranscriptPayload
{
    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("culture")]
    public string Culture { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("text_ts")]
    public long TextTs { get; init; }

    [JsonPropertyName("sentence_id")]
    public long SentenceId { get; init; }

    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; init; }

    [JsonPropertyName("uid")]
    public long Uid { get; init; }

    [JsonPropertyName("words")]
    public List<AgoraTranscriptionWord> Words { get; init; } = [];

    [JsonPropertyName("trans")]
    public List<AgoraTranslationResult>? Trans { get; init; }

    [JsonPropertyName("original_transcript")]
    public AgoraOriginalTranscript? OriginalTranscript { get; init; }
}

internal sealed record AgoraTranscriptionWord
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("is_final")]
    public bool IsFinal { get; init; }
}

internal sealed record AgoraTranslationResult
{
    [JsonPropertyName("is_final")]
    public bool IsFinal { get; init; }

    [JsonPropertyName("lang")]
    public string Lang { get; init; } = string.Empty;

    [JsonPropertyName("texts")]
    public List<string> Texts { get; init; } = [];
}

internal sealed record AgoraOriginalTranscript
{
    [JsonPropertyName("culture")]
    public string Culture { get; init; } = string.Empty;

    [JsonPropertyName("words")]
    public List<AgoraTranscriptionWord> Words { get; init; } = [];
}
