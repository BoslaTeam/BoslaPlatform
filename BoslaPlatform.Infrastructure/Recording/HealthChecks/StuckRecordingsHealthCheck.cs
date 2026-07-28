using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BoslaPlatform.Infrastructure.Recording.HealthChecks;

/// <summary>
/// Reports recordings that entered <see cref="RecordingStatus.Recording"/> and
/// never finalized.
///
/// WHY THIS EXISTS:
///   A recording that silently never completes leaves no error in any log — the
///   pipeline simply stops emitting stages. The only observable symptom is a row
///   that stays in Recording forever. This check turns that silence into a
///   signal, and names the sessions so the correlation ids can be looked up.
/// </summary>
internal sealed class StuckRecordingsHealthCheck : IHealthCheck
{
    /// <summary>
    /// Generous relative to a consultation: anything past this is stuck, not slow.
    /// </summary>
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromHours(4);

    private readonly IAppDbContext _context;

    public StuckRecordingsHealthCheck(IAppDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - StuckThreshold;

        var stuck = await _context.VideoSessions
            .AsNoTracking()
            .Where(s => s.RecordingStatus == RecordingStatus.Recording
                        && s.RecordingStartedAtUtc != null
                        && s.RecordingStartedAtUtc < cutoff)
            .Select(s => new { s.Id, s.AgoraRecordingSid, s.RecordingStartedAtUtc })
            .Take(20)
            .ToListAsync(ct);

        if (stuck.Count == 0)
        {
            return HealthCheckResult.Healthy("No stuck recordings.");
        }

        return HealthCheckResult.Degraded(
            $"{stuck.Count} recording(s) have been in progress for more than {StuckThreshold.TotalHours:N0}h " +
            "and were never finalized.",
            data: new Dictionary<string, object>
            {
                { "stuckCount", stuck.Count },
                {
                    "sessions",
                    string.Join("; ", stuck.Select(s =>
                        $"sessionId={s.Id} correlationId={(string.IsNullOrWhiteSpace(s.AgoraRecordingSid) ? $"sess-{s.Id}" : $"rec-{s.AgoraRecordingSid}")} since={s.RecordingStartedAtUtc:O}"))
                }
            });
    }
}
