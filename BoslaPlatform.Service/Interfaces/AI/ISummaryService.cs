using BoslaPlatform.Shared;
using BoslaPlatform.Domain.Models;

namespace BoslaPlatform.Application.Interfaces.AI;

public interface ISummaryService
{
    Task<Result<SessionSummary>> GetAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RegenerateAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
