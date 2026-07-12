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
}