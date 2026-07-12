using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos;

/// <summary>
/// Domain event raised after a cloud recording stops successfully.
/// Intended to trigger post-processing — e.g. uploading to Cloudflare R2,
/// generating thumbnails, or running transcription.
///
/// The actual upload logic is NOT yet implemented. This event exists as a
/// preparation step so that background job infrastructure can be wired up
/// without waiting for the storage provider integration.
/// </summary>
public sealed class RecordingUploadRequestedEvent : DomainEvent
{
    public Guid SessionId { get; }

    public string? ResourceId { get; }

    public string? Sid { get; }

    public int? DurationSeconds { get; }

    public long? FileSizeBytes { get; }

    public RecordingUploadRequestedEvent(
        Guid sessionId,
        string? resourceId = null,
        string? sid = null,
        int? durationSeconds = null,
        long? fileSizeBytes = null)
    {
        SessionId = sessionId;
        ResourceId = resourceId;
        Sid = sid;
        DurationSeconds = durationSeconds;
        FileSizeBytes = fileSizeBytes;
    }
}
