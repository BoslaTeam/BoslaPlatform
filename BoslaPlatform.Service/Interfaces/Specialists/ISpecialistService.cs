using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Pagination;

namespace BoslaPlatform.Application.Interfaces.Specialists
{
    public interface ISpecialistService
    {
        // Onboarding
        Task<Result<StartResponse>> StartAsync(CancellationToken ct = default);

        // Submission
        Task<Result> SubmitForReviewAsync(CancellationToken ct = default);

        // Documents
        Task<Result<Guid>> UploadDocumentAsync(Guid specialistId, UploadDocumentRequest request, CancellationToken ct = default);
        Task<Result<IReadOnlyList<SpecialistDocumentResponse>>> GetDocumentsAsync(Guid specialistId, CancellationToken ct = default);
        Task<Result> DeleteDocumentAsync(Guid specialistId, Guid documentId, CancellationToken ct = default);

        // Verification
        Task<Result<VerificationDetailsResponse>> GetVerificationAsync(Guid specialistId, CancellationToken ct = default);
        //Task<Result<VerificationStatusResponse>> GetVerificationStatusAsync(Guid specialistId, CancellationToken ct = default);
        Task<Result> SubmitVerificationAsync(Guid specialistId, CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> GetMyProfileAsync(CancellationToken ct = default);

        Task<Result<SpecialistProfileResponse>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default);

        Task<Result<List<AvailabilityResponse>>> GetMyAvailabilityAsync(CancellationToken ct = default);

        Task<Result<IReadOnlyList<Guid>>> AddAvailabilitiesAsync(AddAvailabilitiesRequest request, CancellationToken ct = default);
        Task<Result> DeleteAvailabilityAsync(Guid availabilityId, CancellationToken ct = default);

        Task<Result> AddExpertiseAsync(AddExpertiseRequest request, CancellationToken ct = default);

        Task<Result> DeleteExpertiseAsync(Guid expertiseId, CancellationToken ct = default);

        Task<Result<IReadOnlyList<ExpertiseResponse>>> GetMyExpertiseAsync(CancellationToken ct = default);

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

        Task<Result<IReadOnlyList<SpecialistDocumentResponse>>> GetCertificatesAsync(Guid specialistId, CancellationToken ct = default);

    }
}