using System.Diagnostics;
using System.Diagnostics.Metrics;
using BoslaPlatform.Application.Observability;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// The single emission point for a recording pipeline stage. One call fans out
/// to four sinks that must never disagree:
///   1. a structured log line (human forensics),
///   2. an OpenTelemetry <see cref="Activity"/> span (distributed tracing),
///   3. metrics (aggregate health),
///   4. a persisted event (the queryable timeline).
/// </summary>
internal sealed class RecordingPipelineLog : IRecordingPipelineLog
{
    /// <summary>OTel ActivitySource. Register this name with the tracer provider.</summary>
    public const string ActivitySourceName = "Bosla.Recording";

    private static readonly ActivitySource Activity = new(ActivitySourceName, "1.0.0");

    private readonly ILogger<RecordingPipelineLog> _logger;
    private readonly RecordingPipelineMetrics _metrics;
    private readonly RecordingTelemetryQueue _queue;

    public RecordingPipelineLog(
        ILogger<RecordingPipelineLog> logger,
        RecordingPipelineMetrics metrics,
        RecordingTelemetryQueue queue)
    {
        _logger = logger;
        _metrics = metrics;
        _queue = queue;
    }

    public void Started(RecordingStage stage, RecordingLogContext context)
    {
        using var _ = BeginScope(stage, context);

        _metrics.StageStarted(stage);
        Persist(stage, "Started", context, null, null, null, null);

        _logger.LogInformation(
            "[RecordingPipeline] {Stage} | started | CorrelationId={CorrelationId} | SessionId={SessionId} | " +
            "AppointmentId={AppointmentId} | RecordingId={RecordingId} | ResourceId={ResourceId} | Sid={Sid} | " +
            "TimestampUtc={TimestampUtc:O}",
            stage, context.CorrelationId, Fmt(context.SessionId), Fmt(context.AppointmentId),
            Fmt(context.RecordingId), Fmt(context.ResourceId), Fmt(context.Sid), DateTime.UtcNow);
    }

    public void Succeeded(
        RecordingStage stage,
        RecordingLogContext context,
        TimeSpan? duration = null,
        IReadOnlyDictionary<string, object?>? extra = null)
    {
        using var _ = BeginScope(stage, context);
        using var activity = StartActivity(stage, context, "ok", duration, extra);

        _metrics.StageSucceeded(stage, duration);
        var detail = extra is null ? null : string.Join(", ", extra.Select(kv => $"{kv.Key}={kv.Value}"));
        Persist(stage, "Succeeded", context, duration, null, null, detail);

        _logger.LogInformation(
            "[RecordingPipeline] {Stage} | succeeded | CorrelationId={CorrelationId} | SessionId={SessionId} | " +
            "AppointmentId={AppointmentId} | RecordingId={RecordingId} | ResourceId={ResourceId} | Sid={Sid} | " +
            "DurationMs={DurationMs} | Detail={Detail} | TimestampUtc={TimestampUtc:O}",
            stage, context.CorrelationId, Fmt(context.SessionId), Fmt(context.AppointmentId),
            Fmt(context.RecordingId), Fmt(context.ResourceId), Fmt(context.Sid),
            duration?.TotalMilliseconds ?? 0, detail ?? "-", DateTime.UtcNow);
    }

    public void Failed(
        RecordingStage stage,
        RecordingLogContext context,
        string errorCode,
        string errorDescription,
        Exception? exception = null,
        TimeSpan? duration = null)
    {
        using var _ = BeginScope(stage, context);
        using var activity = StartActivity(stage, context, "error", duration, null);
        activity?.SetStatus(ActivityStatusCode.Error, errorDescription);
        activity?.SetTag("error.code", errorCode);

        _metrics.StageFailed(stage, errorCode, duration);
        Persist(stage, "Failed", context, duration, errorCode, errorDescription, null);

        _logger.LogError(
            exception,
            "[RecordingPipeline] {Stage} | FAILED | CorrelationId={CorrelationId} | SessionId={SessionId} | " +
            "AppointmentId={AppointmentId} | RecordingId={RecordingId} | ResourceId={ResourceId} | Sid={Sid} | " +
            "ErrorCode={ErrorCode} | Error={ErrorDescription} | DurationMs={DurationMs} | TimestampUtc={TimestampUtc:O}",
            stage, context.CorrelationId, Fmt(context.SessionId), Fmt(context.AppointmentId),
            Fmt(context.RecordingId), Fmt(context.ResourceId), Fmt(context.Sid),
            errorCode, errorDescription, duration?.TotalMilliseconds ?? 0, DateTime.UtcNow);
    }

    /// <summary>
    /// One span per stage occurrence, tagged with the canonical correlation id.
    /// A single in-process trace cannot span the whole lifecycle because the
    /// stages arrive on separate HTTP requests (and one from Agora's servers),
    /// so cross-stage linkage is via the recording.correlation_id tag rather than
    /// a shared parent span.
    /// </summary>
    private static Activity? StartActivity(
        RecordingStage stage,
        RecordingLogContext context,
        string outcome,
        TimeSpan? duration,
        IReadOnlyDictionary<string, object?>? extra)
    {
        var activity = Activity.StartActivity($"recording.{stage}", ActivityKind.Internal);
        if (activity is null) return null;

        activity.SetTag("recording.correlation_id", context.CorrelationId);
        activity.SetTag("recording.stage", stage.ToString());
        activity.SetTag("recording.outcome", outcome);
        activity.SetTag("recording.provider", context.Provider);
        activity.SetTag("recording.attempt", context.Attempt);
        if (context.SessionId is not null) activity.SetTag("recording.session_id", context.SessionId);
        if (context.RecordingId is not null) activity.SetTag("recording.recording_id", context.RecordingId);
        if (!string.IsNullOrWhiteSpace(context.Sid)) activity.SetTag("recording.sid", context.Sid);
        if (duration is not null) activity.SetTag("recording.duration_ms", duration.Value.TotalMilliseconds);
        if (extra is not null)
            foreach (var kv in extra)
                activity.SetTag($"recording.{kv.Key}", kv.Value);

        return activity;
    }

    private void Persist(
        RecordingStage stage,
        string outcome,
        RecordingLogContext context,
        TimeSpan? duration,
        string? errorCode,
        string? errorDescription,
        string? detail)
    {
        _queue.Enqueue(new RecordingPipelineEvent
        {
            RecordingCorrelationId = context.RecordingCorrelationId
                ?? (context.RecordingId is not null
                    ? RecordingLogContext.ForRecording(context.RecordingId.Value)
                    : null),
            SessionId = context.SessionId,
            AppointmentId = context.AppointmentId,
            RecordingId = context.RecordingId,
            Provider = context.Provider,
            Stage = stage.ToString(),
            Outcome = outcome,
            Attempt = context.Attempt,
            ResourceId = context.ResourceId,
            Sid = context.Sid,
            ChannelName = context.ChannelName,
            DurationMs = duration?.TotalMilliseconds,
            ErrorCode = errorCode,
            ErrorDescription = Truncate(errorDescription, 2048),
            Detail = Truncate(detail, 4000),
            OccurredAtUtc = DateTime.UtcNow
        });
    }

    private IDisposable? BeginScope(RecordingStage stage, RecordingLogContext context) =>
        _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = context.CorrelationId,
            ["RecordingStage"] = stage.ToString(),
            ["SessionId"] = Fmt(context.SessionId),
            ["AppointmentId"] = Fmt(context.AppointmentId),
            ["RecordingId"] = Fmt(context.RecordingId),
            ["ChannelName"] = Fmt(context.ChannelName),
            ["ResourceId"] = Fmt(context.ResourceId),
            ["Sid"] = Fmt(context.Sid),
            ["Provider"] = context.Provider
        });

    private static string Fmt(Guid? value) => value?.ToString() ?? "-";
    private static string Fmt(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
}

/// <summary>
/// OpenTelemetry-compatible metrics for the recording pipeline, on a plain
/// <see cref="Meter"/> so no vendor dependency is introduced. Register meter
/// name "Bosla.Recording" with the OTel MeterProvider (Prometheus scrapes it).
/// </summary>
public sealed class RecordingPipelineMetrics : IDisposable
{
    public const string MeterName = "Bosla.Recording";

    private readonly Meter _meter;
    private readonly Counter<long> _stageStarted;
    private readonly Counter<long> _stageSucceeded;
    private readonly Counter<long> _stageFailed;
    private readonly Histogram<double> _stageDuration;
    private readonly Histogram<double> _webhookLatency;
    private readonly RecordingTelemetryQueue _queue;

    private long _activeRecordings;

    public RecordingPipelineMetrics(RecordingTelemetryQueue queue)
    {
        _queue = queue;
        _meter = new Meter(MeterName, "1.0.0");

        _stageStarted = _meter.CreateCounter<long>(
            "recording.stage.started", "count", "Recording pipeline stages entered.");
        _stageSucceeded = _meter.CreateCounter<long>(
            "recording.stage.succeeded", "count", "Recording pipeline stages completed successfully.");
        _stageFailed = _meter.CreateCounter<long>(
            "recording.stage.failed", "count", "Recording pipeline stages that failed.");
        _stageDuration = _meter.CreateHistogram<double>(
            "recording.stage.duration", "ms", "Duration of a recording pipeline stage.");
        _webhookLatency = _meter.CreateHistogram<double>(
            "recording.webhook.latency", "ms", "Processing latency of a recording webhook.");

        // Active recordings: Start succeeded, not yet Stopped. In-process gauge —
        // resets on restart and is per-instance; the stuck-recordings health check
        // provides the DB-authoritative view.
        _meter.CreateObservableGauge(
            "recording.active", () => Interlocked.Read(ref _activeRecordings),
            "count", "Recordings currently in progress (Start succeeded, not yet Stopped).");

        // Telemetry events dropped because the write queue was full.
        _meter.CreateObservableCounter(
            "recording.telemetry.dropped", () => _queue.DroppedCount,
            "count", "Pipeline telemetry events dropped due to a full queue.");
    }

    public void StageStarted(RecordingStage stage) =>
        _stageStarted.Add(1, Tag(stage));

    public void StageSucceeded(RecordingStage stage, TimeSpan? duration)
    {
        _stageSucceeded.Add(1, Tag(stage));

        if (duration is not null)
        {
            _stageDuration.Record(duration.Value.TotalMilliseconds, Tag(stage));
            if (stage == RecordingStage.WebhookReceived)
                _webhookLatency.Record(duration.Value.TotalMilliseconds);
        }

        if (stage == RecordingStage.Start)
            Interlocked.Increment(ref _activeRecordings);
        else if (stage == RecordingStage.Stop)
            DecrementActive();
    }

    public void StageFailed(RecordingStage stage, string errorCode, TimeSpan? duration)
    {
        _stageFailed.Add(1, Tag(stage), new KeyValuePair<string, object?>("error.code", errorCode));
        if (duration is not null)
            _stageDuration.Record(duration.Value.TotalMilliseconds, Tag(stage));

        // A failed Start never incremented; a failed Stop still ends the recording.
        if (stage == RecordingStage.Stop)
            DecrementActive();
    }

    private void DecrementActive()
    {
        // Clamp at zero: a Stop with no matching Start (restart, replay) must not
        // drive the gauge negative.
        long current, updated;
        do
        {
            current = Interlocked.Read(ref _activeRecordings);
            if (current == 0) return;
            updated = current - 1;
        } while (Interlocked.CompareExchange(ref _activeRecordings, updated, current) != current);
    }

    private static KeyValuePair<string, object?> Tag(RecordingStage stage) =>
        new("stage", stage.ToString());

    public void Dispose() => _meter.Dispose();
}
