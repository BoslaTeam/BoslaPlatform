using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoslaPlatform.Application.Features.VideoSessions.Requests
{
    /// <summary>
    /// Reads a JSON number or a quoted numeric string as <see cref="long"/>.
    /// A rejected payload is worse than a lenient one here: the controller turns
    /// any deserialization failure into a 400, so a type mismatch would drop the
    /// event entirely rather than degrade.
    /// </summary>
    internal sealed class FlexibleLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String => long.TryParse(reader.GetString(), out var parsed) ? parsed : 0,
                JsonTokenType.Null => 0,
                _ => 0
            };

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public sealed class AgoraWebhookPayload
    {
        /// <summary>
        /// RTC channel/user events name this "channelName"; Cloud Recording events
        /// name it "cname". Both are accepted so one model covers both products —
        /// binding only one of them silently yields an empty channel and the
        /// session lookup then fails for every event of the other kind.
        /// </summary>
        [JsonPropertyName("channelName")]
        public string? ChannelNameRaw { get; init; }

        [JsonPropertyName("cname")]
        public string? Cname { get; init; }

        [JsonIgnore]
        public string ChannelName => ChannelNameRaw ?? Cname ?? string.Empty;

        /// <summary>
        /// Sent as a JSON number by some events and as a quoted string by others,
        /// so it is read leniently rather than assuming one form.
        /// </summary>
        [JsonPropertyName("uid")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long Uid { get; init; }

        [JsonPropertyName("ts")]
        public long Ts { get; init; }

        [JsonPropertyName("sid")]
        public string Sid { get; init; } = string.Empty;

        [JsonPropertyName("sequence")]
        public int Sequence { get; init; }

        [JsonPropertyName("details")]
        public AgoraWebhookRecordingDetails? Details { get; init; }
    }

    /// <summary>
    /// Recording-specific details within an Agora webhook payload.
    /// Populated for recording_started and recording_stopped events.
    /// </summary>
    public sealed class AgoraWebhookRecordingDetails
    {
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; init; }

        [JsonPropertyName("sid")]
        public string? Sid { get; init; }

        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; init; }

        [JsonPropertyName("duration")]
        public int? Duration { get; init; }

        [JsonPropertyName("fileSize")]
        public long? FileSize { get; init; }
    }
}

