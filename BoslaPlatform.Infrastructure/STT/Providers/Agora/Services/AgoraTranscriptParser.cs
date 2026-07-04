using System.Collections.Concurrent;
using System.Text.Json;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Utilities;
using BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Transcripts;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Services;

internal sealed class AgoraTranscriptParser
{
    private readonly ConcurrentDictionary<long, byte> _seenSentenceIds = new();
    private readonly ILogger<AgoraTranscriptParser> _logger;

    public AgoraTranscriptParser(ILogger<AgoraTranscriptParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal Result<TranscriptSegment> ParseTranscript(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogWarning("Empty transcript payload received");
            return Error.Validation("Agora.STT.Transcript.Empty", "Transcript payload is empty.");
        }

        AgoraTranscriptPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<AgoraTranscriptPayload>(rawJson, RecordingJsonDefaults.Options)!;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed transcript payload received");
            return Error.Unexpected("Agora.STT.Transcript.Malformed", "Failed to deserialize transcript payload.");
        }

        if (payload is null)
        {
            _logger.LogWarning("Malformed transcript payload: deserialized to null");
            return Error.Unexpected("Agora.STT.Transcript.Null", "Transcript payload deserialized to null.");
        }

        if (string.IsNullOrWhiteSpace(payload.DataType))
        {
            _logger.LogWarning("Transcript payload missing data_type");
            return Error.Validation("Agora.STT.Transcript.MissingDataType", "Transcript payload is missing data_type.");
        }

        _logger.LogInformation(
            "Transcript received, data_type={DataType}, sentence_id={SentenceId}, culture={Culture}",
            payload.DataType, payload.SentenceId, payload.Culture);

        if (payload.DataType == "translate")
        {
            _logger.LogInformation(
                "Skipping translate event (data_type=translate) for sentence_id={SentenceId}",
                payload.SentenceId);
            return Error.Validation("Agora.STT.Transcript.SkipTranslate", "Translate events are handled separately.");
        }

        if (payload.DataType != "transcribe")
        {
            _logger.LogWarning(
                "Unknown transcript data_type={DataType} for sentence_id={SentenceId}",
                payload.DataType, payload.SentenceId);
            return Error.Validation("Agora.STT.Transcript.UnknownDataType", $"Unknown data_type: {payload.DataType}.");
        }

        if (payload.Words is null || payload.Words.Count == 0)
        {
            _logger.LogWarning(
                "Transcribe payload with no words, sentence_id={SentenceId}",
                payload.SentenceId);
            return Error.Validation("Agora.STT.Transcript.NoWords", "Transcribe payload contains no words.");
        }

        if (!_seenSentenceIds.TryAdd(payload.SentenceId, 0))
        {
            _logger.LogWarning(
                "Duplicate transcript event detected, sentence_id={SentenceId}",
                payload.SentenceId);
            return Error.Validation("Agora.STT.Transcript.Duplicate", $"Duplicate sentence_id: {payload.SentenceId}.");
        }

        var text = string.Join(" ", payload.Words.Select(w => w.Text));
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning(
                "Transcribe payload with empty text, sentence_id={SentenceId}",
                payload.SentenceId);
            return Error.Validation("Agora.STT.Transcript.EmptyText", "Transcribe payload contains empty text.");
        }

        var isFinal = payload.Words.All(w => w.IsFinal);
        var timestamp = payload.TextTs > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(payload.TextTs)
            : DateTimeOffset.UtcNow;

        var segment = new TranscriptSegment
        {
            SequenceNumber = payload.SentenceId,
            SpeakerId = payload.Uid > 0 ? payload.Uid.ToString() : null,
            Text = text,
            IsFinal = isFinal,
            Language = payload.Culture ?? string.Empty,
            TimestampUtc = timestamp
        };

        if (isFinal)
        {
            _logger.LogInformation(
                "Final transcript for sentence_id={SentenceId}: \"{Text}\" (speaker={SpeakerId}, language={Language})",
                segment.SequenceNumber, segment.Text, segment.SpeakerId, segment.Language);
        }
        else
        {
            _logger.LogInformation(
                "Partial transcript for sentence_id={SentenceId}: \"{Text}\" (speaker={SpeakerId}, language={Language})",
                segment.SequenceNumber, segment.Text, segment.SpeakerId, segment.Language);
        }

        return Result<TranscriptSegment>.Success(segment);
    }

    internal void Reset()
    {
        _seenSentenceIds.Clear();
        _logger.LogDebug("Transcript parser deduplication state reset");
    }
}
