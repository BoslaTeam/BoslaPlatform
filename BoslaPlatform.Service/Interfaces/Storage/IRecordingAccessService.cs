using BoslaPlatform.Application.Features.RecordingAccess.Dtos;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Storage;

public interface IRecordingAccessService
{
    Task<Result<RecordingWatchResponse>> GetWatchUrlAsync(
        Guid sessionId,
        Guid userId,
        string userRole,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates authorization then opens a lazy, non-buffered read stream for the recording.
    /// The <see cref="RecordingDownloadContext.Content"/> stream must be disposed by the caller
    /// (the HTTP framework handles this automatically when used with <c>Results.Stream</c>).
    /// </summary>
    Task<Result<RecordingDownloadContext>> GetDownloadStreamAsync(
        Guid sessionId,
        Guid userId,
        string userRole,
        CancellationToken ct = default);
}