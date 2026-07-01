using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Admin.DTOs;

namespace BoslaPlatform.Application.Features.Admin.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}
