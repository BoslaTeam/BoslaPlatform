namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// Public OTel identifiers for the recording pipeline, so the composition root
/// can register them with the tracer/meter providers without depending on the
/// internal emitter type.
/// </summary>
public static class RecordingObservabilityNames
{
    public const string ActivitySource = "Bosla.Recording";
    public const string Meter = "Bosla.Recording";
}

/// <summary>
/// A persisted pipeline stage occurrence. This is telemetry, not domain state —
/// it lives in its own table written from a dedicated context so that a stage
/// failure is recorded even when the domain transaction that triggered it rolls
/// back. Append-only; never updated.
/// </summary>
public sealed class RecordingPipelineEvent
{
    public long Id { get; set; }

    /// <summary>Canonical vendor-neutral correlation id, when known at emit time.</summary>
    public string? RecordingCorrelationId { get; set; }

    public Guid? SessionId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? RecordingId { get; set; }

    public string Provider { get; set; } = "Agora";
    public string Stage { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;   // Started | Succeeded | Failed
    public int Attempt { get; set; } = 1;

    public string? ResourceId { get; set; }
    public string? Sid { get; set; }
    public string? ChannelName { get; set; }

    public double? DurationMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
    public string? Detail { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
