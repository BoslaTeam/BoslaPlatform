using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface ISpecialistService
    {
        Task<Result<SpecialistOnboardResponse>> OnboardAsync(SpecialistOnboardRequest request, CancellationToken ct = default);
    }
}