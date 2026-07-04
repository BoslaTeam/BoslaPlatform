namespace BoslaPlatform.Application.Interfaces.Video;

public sealed record TranscriptSegment
{
    /// <summary>
    /// Monotonically increasing sequence within the transcript stream.
    /// Used to order transcript segments.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// Provider-independent speaker identifier.
    /// May be null when speaker identification is unavailable.
    /// </summary>
    public string? SpeakerId { get; init; }

    /// <summary>
    /// Human-readable speaker label.
    /// Examples: Doctor, Patient, Unknown.
    /// </summary>
    public string? SpeakerLabel { get; init; }

    /// <summary>
    /// Transcript text.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the final version of the transcript segment.
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// Language of this transcript segment.
    /// Examples: ar-EG, en-US.
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the provider produced this transcript segment.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>
    /// Offset from the beginning of the session.
    /// Useful for replay and AI references.
    /// </summary>
    public TimeSpan Offset { get; init; }
}