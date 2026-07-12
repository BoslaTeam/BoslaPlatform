using BoslaPlatform.Application.Interfaces.Storage;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingMetricsTests
{
    [Fact]
    public void NoOp_recording_metrics_does_not_throw()
    {
        var metrics = new NoOpRecordingMetrics();
        var exception = Record.Exception(() =>
        {
            metrics.RecordUploadDuration(TimeSpan.FromSeconds(5));
            metrics.RecordUploadSuccess(1024, TimeSpan.FromSeconds(3));
            metrics.RecordUploadFailure("Storage.Error");
            metrics.RecordRetry();
            metrics.IncrementActiveUploads();
            metrics.DecrementActiveUploads();
            metrics.RecordPresignedUrlGenerated();
        });
        Assert.Null(exception);
    }
}