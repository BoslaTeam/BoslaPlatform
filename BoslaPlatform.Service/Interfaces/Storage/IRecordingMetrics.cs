namespace BoslaPlatform.Application.Interfaces.Storage;

public interface IRecordingMetrics
{
    void RecordUploadDuration(TimeSpan duration);
    void RecordUploadSuccess(long contentLength, TimeSpan duration);
    void RecordUploadFailure(string errorCode);
    void RecordRetry();
    void IncrementActiveUploads();
    void DecrementActiveUploads();
    void RecordPresignedUrlGenerated();
}

public sealed class NoOpRecordingMetrics : IRecordingMetrics
{
    public void RecordUploadDuration(TimeSpan duration) { }
    public void RecordUploadSuccess(long contentLength, TimeSpan duration) { }
    public void RecordUploadFailure(string errorCode) { }
    public void RecordRetry() { }
    public void IncrementActiveUploads() { }
    public void DecrementActiveUploads() { }
    public void RecordPresignedUrlGenerated() { }
}