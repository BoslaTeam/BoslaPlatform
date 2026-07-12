using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos;

public sealed class RecordingUploadFailedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string? ObjectKey { get; }
    public string? BucketName { get; }
    public string FailureReason { get; }
    public int Attempts { get; }

    public RecordingUploadFailedEvent(
        Guid sessionId,
        string? objectKey,
        string? bucketName,
        string failureReason,
        int attempts)
    {
        SessionId = sessionId;
        ObjectKey = objectKey;
        BucketName = bucketName;
        FailureReason = failureReason;
        Attempts = attempts;
    }
}