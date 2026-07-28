using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video;

/// <summary>
/// THE single source of truth for finishing a recording. Every completion path —
/// manual Stop, automatic expiration, specialist End, and the Agora webhooks
/// (uploaded / recorder_leave / session_exit) — funnels through
/// <see cref="CompleteAsync"/> so they all run the identical pipeline:
///
///   (optionally) provider Stop → resolve object key → verify upload (Query poll +
///   S3 HeadObject) → persist S3 metadata → set final DB state.
///
/// There is no completion, verification, or metadata-persistence logic anywhere
/// else. The method is idempotent: a recording that is already Completed (or has
/// no recording to finish) returns success without side effects, so duplicate
/// webhooks and completion races are safe.
/// </summary>
public interface IRecordingCompletionService
{
    Task<Result<RecordingCompletionOutcome>> CompleteAsync(
        Guid videoSessionId,
        RecordingCompletionTrigger trigger,
        RecordingProviderHint? hint = null,
        CancellationToken ct = default);
}

/// <summary>
/// How the completion was initiated. Determines whether the pipeline issues a
/// provider Stop (the recording is still running) or only verifies (the provider
/// already reported the recording finished).
/// </summary>
public enum RecordingCompletionTrigger
{
    /// <summary>Specialist clicked Stop — recording is running; issue provider Stop, then verify (polls).</summary>
    ManualStop,

    /// <summary>Expiration / channel destroyed / specialist End — issue provider Stop, then verify (polls).</summary>
    SessionEnded,

    /// <summary>Agora webhook already confirmed the recording finished — do NOT Stop; verify once (no poll).</summary>
    WebhookConfirmed
}

/// <summary>
/// Optional identifiers a webhook already knows (the RTC/Query path does not),
/// used to seed verification without an extra round-trip.
/// </summary>
public sealed record RecordingProviderHint(
    string? ObjectKey = null,
    AgoraUploadingStatus UploadingStatus = AgoraUploadingStatus.Unknown,
    int? DurationSeconds = null,
    long? FileSizeBytes = null);

public sealed record RecordingCompletionOutcome(
    RecordingStatus FinalStatus,
    UploadVerificationOutcome VerificationOutcome,
    string? ObjectKey = null);
