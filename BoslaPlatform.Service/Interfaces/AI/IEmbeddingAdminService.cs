using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.AI;

public interface IEmbeddingAdminService
{
    Task<Result<object>> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> RebuildAllAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> RebuildSelfAsync(CancellationToken cancellationToken = default);
}
