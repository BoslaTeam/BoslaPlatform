using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Observability
{
    public interface IRecordingDiagnosticsService
    {
        /// <summary>
        /// Reconstructs the full lifecycle timeline for a canonical correlation id
        /// (rec-{recordingId}) or a raw recording/session id. Resolves the join
        /// keys the provider stages need, then queries the persisted event store.
        /// </summary>
        Task<Result<RecordingTimeline>> GetTimelineAsync(string correlationId, CancellationToken ct = default);
    }

    public sealed class RecordingDiagnosticsService : IRecordingDiagnosticsService
    {
        private readonly IAppDbContext _context;
        private readonly IRecordingTimelineStore _store;

        public RecordingDiagnosticsService(IAppDbContext context, IRecordingTimelineStore store)
        {
            _context = context;
            _store = store;
        }

        public async Task<Result<RecordingTimeline>> GetTimelineAsync(
            string correlationId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                return Error.Validation("Diagnostics.MissingCorrelationId", "A correlation id is required.");

            // Accept "rec-{guid}", a bare recording/session guid, or a channel name.
            var normalized = correlationId.Trim();
            var idPart = normalized.StartsWith("rec-", StringComparison.OrdinalIgnoreCase)
                ? normalized[4..]
                : normalized;

            RecordingTimelineJoinKeys joinKeys;

            if (Guid.TryParse(idPart, out var id))
            {
                // Resolve the recording's accumulated provider identifiers so that
                // provider-emitted stages (which never saw our id) are stitched in.
                var recording = await _context.ScreenRecordings
                    .AsNoTracking()
                    .Where(r => r.Id == id || r.VideoSessionId == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.VideoSessionId,
                        r.AgoraRecordingSid,
                        r.AgoraRecordingId
                    })
                    .FirstOrDefaultAsync(ct);

                if (recording is not null)
                {
                    var channel = await _context.VideoSessions
                        .AsNoTracking()
                        .Where(s => s.Id == recording.VideoSessionId)
                        .Select(s => s.AgoraChannelName)
                        .FirstOrDefaultAsync(ct);

                    normalized = RecordingLogContext.ForRecording(recording.Id);
                    joinKeys = new RecordingTimelineJoinKeys
                    {
                        RecordingId = recording.Id,
                        SessionId = recording.VideoSessionId,
                        Sid = recording.AgoraRecordingSid,
                        ResourceId = recording.AgoraRecordingId,
                        ChannelName = channel
                    };
                }
                else
                {
                    // No aggregate found; still return whatever telemetry matches
                    // the id/correlation id directly.
                    joinKeys = new RecordingTimelineJoinKeys { RecordingId = id, SessionId = id };
                }
            }
            else
            {
                joinKeys = new RecordingTimelineJoinKeys { ChannelName = normalized };
            }

            var timeline = await _store.GetTimelineAsync(normalized, joinKeys, ct);
            return Result<RecordingTimeline>.Success(timeline);
        }
    }
}
