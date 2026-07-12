using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Domain.Entities.System;
using BoslaPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Storage;

/// <summary>
/// Persists immutable audit records for recording-related user actions.
/// Never stores presigned URLs or other sensitive content.
/// </summary>
public sealed class RecordingAuditService : IRecordingAuditService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<RecordingAuditService> _logger;

    public RecordingAuditService(
        IAppDbContext context,
        ILogger<RecordingAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        Guid videoSessionId,
        Guid? userId,
        RecordingAuditAction action,
        CancellationToken ct = default)
    {
        try
        {
            var entry = RecordingAuditLog.Create(videoSessionId, userId, action);
            _context.RecordingAuditLogs.Add(entry);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Audit logged: Action={Action}, VideoSessionId={VideoSessionId}, UserId={UserId}",
                action, videoSessionId, userId);
        }
        catch (Exception ex)
        {
            // Audit failure must never surface to the caller.
            // Log the error and continue — recording access should not be blocked by audit.
            _logger.LogError(ex,
                "Audit log failed for Action={Action}, VideoSessionId={VideoSessionId}, UserId={UserId}",
                action, videoSessionId, userId);
        }
    }
}
