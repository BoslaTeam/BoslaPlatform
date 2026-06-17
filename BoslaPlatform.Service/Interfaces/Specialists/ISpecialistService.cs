using BoslaPlatform.Application.Features.Specialists.DTOs;
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

        Task<Result> AddExpertiseAsync(AddExpertiseRequest request, CancellationToken ct = default);

        Task<Result> DeleteExpertiseAsync(Guid expertiseId, CancellationToken ct = default);

        Task<Result<bool>> UpdateCancellationPolicyAsync(UpdateCancellationPolicyRequest request,CancellationToken ct = default);
        Task<Result<bool>> UpdateBookingPolicyAsync(UpdateBookingPolicyRequest request,CancellationToken ct);

        Task<Result<IReadOnlyList<ExperienceDto>>> GetExperienceAsync(CancellationToken ct);


        Task<Result<Guid>> AddExperienceAsync( AddExperienceRequestDTO request,CancellationToken ct);
        Task<Result<bool>> UpdateExperienceAsync( Guid experienceId,UpdateExperienceRequest request, CancellationToken ct);

        Task<Result> DeleteExperienceAsync(Guid experienceId, CancellationToken ct);
    
   






    }
}