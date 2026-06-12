using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface ISpecialistService
    {
        Task<Result<SpecialistOnboardResponse>> OnboardAsync(SpecialistOnboardRequest request, CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> GetMyProfileAsync(CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default);

        Task<Result<List<AvailabilityResponse>>> GetMyAvailabilityAsync(CancellationToken ct = default);

        Task<Result<Guid>> AddAvailabilityAsync(AddAvailabilityRequest request, CancellationToken ct = default);

        Task<Result> DeleteAvailabilityAsync(Guid availabilityId, CancellationToken ct = default);
    }
}