using BoslaPlatform.Domain.Common;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Domain.Models.Video
{
    public class TranscriptSegment : AuditableEntity
    {
        private TranscriptSegment() { }

        public long SequenceNumber { get; private set; }
        public string? SpeakerId { get; private set; }
        public string? SpeakerLabel { get; private set; }
        public string TranscriptText { get; private set; } = string.Empty;
        public string Language { get; private set; } = string.Empty;
        public DateTimeOffset TimestampUtc { get; private set; }
        public TimeSpan Offset { get; private set; }

        public Guid VideoSessionId { get; private set; }
        public VideoSession VideoSession { get; private set; } = null!;

        public static Result<TranscriptSegment> Create(
            Guid videoSessionId,
            long sequenceNumber,
            string transcriptText,
            string language,
            string? speakerId,
            string? speakerLabel,
            DateTimeOffset timestampUtc,
            TimeSpan offset)
        {
            if (videoSessionId == Guid.Empty)
            {
                return Error.Validation(
                    "TranscriptSegment.InvalidSessionId",
                    "Video session identifier is required.");
            }

            if (sequenceNumber < 0)
            {
                return Error.Validation(
                    "TranscriptSegment.InvalidSequenceNumber",
                    "Sequence number must be non-negative.");
            }

            if (string.IsNullOrWhiteSpace(transcriptText))
            {
                return Error.Validation(
                    "TranscriptSegment.TextRequired",
                    "Transcript text is required.");
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                return Error.Validation(
                    "TranscriptSegment.LanguageRequired",
                    "Language is required.");
            }

            if (timestampUtc == default)
            {
                return Error.Validation(
                    "TranscriptSegment.InvalidTimestamp",
                    "Timestamp must be a valid date and time.");
            }

            return new TranscriptSegment
            {
                VideoSessionId = videoSessionId,
                SequenceNumber = sequenceNumber,
                TranscriptText = transcriptText,
                Language = language,
                SpeakerId = speakerId,
                SpeakerLabel = speakerLabel,
                TimestampUtc = timestampUtc,
                Offset = offset
            };
        }
    }
}
