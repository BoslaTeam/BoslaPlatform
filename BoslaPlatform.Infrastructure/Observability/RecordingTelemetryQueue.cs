using System.Threading.Channels;

namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// Bounded in-process queue between stage emission and persistence.
///
/// Telemetry must never slow down or fail the recording pipeline, so events are
/// handed off non-blocking here and flushed by a background writer. If the queue
/// is full (writer stalled), the event is dropped and counted rather than
/// blocking a caller — observability degrades before the pipeline does.
/// </summary>
public sealed class RecordingTelemetryQueue
{
    private readonly Channel<RecordingPipelineEvent> _channel =
        Channel.CreateBounded<RecordingPipelineEvent>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    private long _dropped;

    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <summary>Non-blocking enqueue. Increments the drop counter if full.</summary>
    public void Enqueue(RecordingPipelineEvent evt)
    {
        if (!_channel.Writer.TryWrite(evt))
        {
            Interlocked.Increment(ref _dropped);
        }
    }

    public IAsyncEnumerable<RecordingPipelineEvent> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
