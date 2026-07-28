using BoslaPlatform.Application.Observability;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Observability;

/// <summary>
/// Reconstructs a recording timeline from the persisted event store.
///
/// Stages emitted by the provider (Acquire/Start/Stop) never carry our recording
/// id — the provider is vendor-neutral and only knows the channel/resource/SID.
/// This store stitches them back onto the recording using every join key the
/// caller could resolve, so the timeline is whole regardless of which identifier
/// a given stage happened to know.
/// </summary>
internal sealed class SqlRecordingTimelineStore : IRecordingTimelineStore
{
    private readonly RecordingDiagnosticsDbContext _db;

    public SqlRecordingTimelineStore(RecordingDiagnosticsDbContext db) => _db = db;

    public async Task<RecordingTimeline> GetTimelineAsync(
        string correlationId,
        RecordingTimelineJoinKeys joinKeys,
        CancellationToken ct = default)
    {
        var sid = joinKeys.Sid;
        var resourceId = joinKeys.ResourceId;
        var channel = joinKeys.ChannelName;
        var recordingId = joinKeys.RecordingId;

        var rows = await _db.RecordingPipelineEvents
            .AsNoTracking()
            .Where(e =>
                e.RecordingCorrelationId == correlationId
                || (recordingId != null && e.RecordingId == recordingId)
                || (sid != null && e.Sid == sid)
                || (resourceId != null && e.ResourceId == resourceId)
                || (channel != null && e.ChannelName == channel))
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);

        var entries = rows.Select(e => new RecordingTimelineEntry
        {
            Stage = e.Stage,
            Outcome = e.Outcome,
            OccurredAtUtc = e.OccurredAtUtc,
            Provider = e.Provider,
            Attempt = e.Attempt,
            DurationMs = e.DurationMs,
            SessionId = e.SessionId,
            RecordingId = e.RecordingId,
            ResourceId = e.ResourceId,
            Sid = e.Sid,
            ChannelName = e.ChannelName,
            ErrorCode = e.ErrorCode,
            ErrorDescription = e.ErrorDescription,
            Detail = e.Detail
        }).ToList();

        var failed = rows.LastOrDefault(e => e.Outcome == "Failed");
        var furthest = rows
            .Where(e => e.Outcome == "Succeeded")
            .Select(e => e.Stage)
            .LastOrDefault();

        string verdict;
        if (rows.Count == 0)
            verdict = "NoData";
        else if (furthest == nameof(RecordingStage.MetadataSaved)
                 || furthest == nameof(RecordingStage.PresignedUrlGenerated))
            verdict = "Healthy";
        else if (failed is not null)
            verdict = $"Failed@{failed.Stage}";
        else
            verdict = "InProgress";

        return new RecordingTimeline
        {
            CorrelationId = correlationId,
            Entries = entries,
            FurthestStageReached = furthest,
            FailedStage = failed?.Stage,
            Verdict = verdict
        };
    }
}
