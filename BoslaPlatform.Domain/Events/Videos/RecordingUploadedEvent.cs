using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Videos;

public sealed class RecordingUploadedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string ObjectKey { get; }
    public string BucketName { get; }
    public long ContentLength { get; }
    public TimeSpan UploadDuration { get; }

    public RecordingUploadedEvent(
        Guid sessionId,
        string objectKey,
        string bucketName,
        long contentLength,
        TimeSpan uploadDuration)
    {
        SessionId = sessionId;
        ObjectKey = objectKey;
        BucketName = bucketName;
        ContentLength = contentLength;
        UploadDuration = uploadDuration;
    }
}