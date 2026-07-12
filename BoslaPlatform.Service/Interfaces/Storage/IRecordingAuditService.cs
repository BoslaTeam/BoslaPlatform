using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Interfaces.Storage;

/// <summary>
/// Writes immutable audit log entries for recording-related user actions.
/// Implementations must NEVER store presigned URLs or other sensitive data.
/// </summary>
public interface IRecordingAuditService
{
    /// <summary>
    /// Persists an audit record for the given action.
    /// This is a fire-and-log operation — exceptions are caught and logged by the implementation.
    /// </summary>
    Task LogAsync(
        Guid videoSessionId,
        Guid? userId,
        RecordingAuditAction action,
        CancellationToken ct = default);
}
