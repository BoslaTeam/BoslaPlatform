namespace BoslaPlatform.Application.Interfaces.Storage;

public interface IRecordingMetrics
{
    // Upload metrics
    void RecordUploadDuration(TimeSpan duration);
    void RecordUploadSuccess(long contentLength, TimeSpan duration);
    void RecordUploadFailure(string errorCode);
    void RecordRetry();
    void IncrementActiveUploads();
    void DecrementActiveUploads();

    // Access metrics
    void RecordPresignedUrlGenerated();
    void RecordDownloadDuration(TimeSpan duration);

    // Storage metrics
    void RecordAverageRecordingSize(long sizeBytes);
    void RecordPendingUploads(int count);
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
    public void RecordDownloadDuration(TimeSpan duration) { }
    public void RecordAverageRecordingSize(long sizeBytes) { }
    public void RecordPendingUploads(int count) { }
}