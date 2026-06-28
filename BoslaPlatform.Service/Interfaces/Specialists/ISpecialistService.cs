using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;

namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface ISpecialistService
    {
        Task<Result<SpecialistOnboardResponse>> OnboardAsync(SpecialistOnboardRequest request, CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> GetMyProfileAsync(CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default);

        Task<Result<List<AvailabilityResponse>>> GetMyAvailabilityAsync(CancellationToken ct = default);

        Task<Result<IReadOnlyList<Guid>>> AddAvailabilitiesAsync(AddAvailabilitiesRequest request, CancellationToken ct = default);
        Task<Result> DeleteAvailabilityAsync(Guid availabilityId, CancellationToken ct = default);

        Task<Result> AddExpertiseAsync(AddExpertiseRequest request, CancellationToken ct = default);

        Task<Result> DeleteExpertiseAsync(Guid expertiseId, CancellationToken ct = default);

        Task<Result<bool>> UpdateCancellationPolicyAsync(UpdateCancellationPolicyRequest request, CancellationToken ct = default);
        Task<Result<bool>> UpdateBookingPolicyAsync(UpdateBookingPolicyRequest request, CancellationToken ct);

        Task<Result<IReadOnlyList<ExperienceDto>>> GetExperienceAsync(CancellationToken ct);


        Task<Result<IReadOnlyList<Guid>>> AddExperiencesAsync(AddExperiencesRequest request, CancellationToken ct);
        Task<Result<bool>> UpdateExperienceAsync(Guid experienceId, UpdateExperienceRequest request, CancellationToken ct);

        Task<Result> DeleteExperienceAsync(Guid experienceId, CancellationToken ct);



        Task<Result> AddSkillsAsync(AddSkillRequest request, CancellationToken ct);


        Task<Result> DeleteSkillAsync(Guid skillId, CancellationToken ct);


        Task<Result> AddToolsAsync(AddToolRequest request, CancellationToken ct);

        Task<Result> DeleteToolAsync(Guid toolId, CancellationToken ct);
        Task<Result<PaginatedResult<SpecialistListItemResponse>>> GetSpecialistsAsync(GetSpecialistsRequest request, CancellationToken ct);
        Task<Result<SpecialistDetailsResponse>> GetSpecialistByIdAsync(Guid specialistId, CancellationToken ct);
        Task<Result<SpecialistEarningsDto>> GetEarningsAsync(CancellationToken ct = default);

        Task<Result<IReadOnlyList<SpecialistAvailabilityResponse>>> GetSpecialistAvailabilityAsync(Guid specialistId, CancellationToken ct);

        Task<Result<IReadOnlyList<SpecialistReviewResponse>>> GetSpecialistReviewsAsync(Guid specialistId, CancellationToken ct);

        Task<Result<SpecialistDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<Result<SpecialistReviewsResponse>> GetReviewsAsync(Guid specialistId, int pageNumber, int pageSize, CancellationToken ct = default);

        Task<Result<SpecialistReviewsResponse>> GetMyReviewsAsync(int pageNumber, int pageSize, CancellationToken ct = default);

        Task<Result<IReadOnlyList<SkillResponse>>> GetSkillsAsync(CancellationToken ct = default);

        Task<Result<IReadOnlyList<ToolResponse>>> GetToolsAsync(CancellationToken ct = default);




    }
}