namespace BoslaPlatform.Application.Interfaces.Storage;

/// <summary>
/// Scans recordings that are stuck in Pending/Retrying states and re-triggers upload.
/// Implementations must be idempotent — calling this method multiple times is safe.
/// </summary>
public interface IRecordingReconciliationService
{
    /// <summary>
    /// Performs one reconciliation pass:
    /// 1. Queries recordings where UploadStatus ∈ {Pending, Retrying} and NextRetryAtUtc ≤ UtcNow.
    /// 2. For each eligible recording, queries Agora for file availability.
    /// 3. If files are available, triggers RecordingTransferService.
    /// 4. If RetryCount ≥ MaxRetryAttempts, marks the recording as Cancelled.
    /// </summary>
    Task ReconcileAsync(CancellationToken ct = default);
}
