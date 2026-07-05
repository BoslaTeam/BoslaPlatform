using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Portfolio.DTOs;
using BoslaPlatform.Application.Features.Portfolio.Requests;
using BoslaPlatform.Application.Features.Portfolio.Services;
using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.AI.Gemini;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/specialists")]
    [Authorize]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class SpecialistsController(ISpecialistService specialistService, IEmbeddingAdminService embeddingAdmin, IAppDbContext context, IPortfolioService portfolioService) : ControllerBase
    {
        private async Task<Guid> GetCurrentSpecialistIdAsync()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Guid.Empty;

            var specialistId = await context.Specialists
                .Where(s => s.UserId == userId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            return specialistId ?? Guid.Empty;
        }
        #region Onboard & Profile 

        [HttpPost("me/start")]
        [ProducesResponseType(typeof(ApiResponse<StartResponse>), StatusCodes.Status200OK)]
        public async Task<IResult> Start(CancellationToken ct)
        {
            var result = await specialistService.StartAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<StartResponse>.SuccessResponse(
                        value, "Specialist profile initialized successfully.")),
                errors => errors.ToProblem());
        }

        [HttpPost("me/embedding/refresh")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IResult> RefreshMyEmbeddings(CancellationToken ct)
        {
            var result = await embeddingAdmin.RebuildSelfAsync(ct);
            if (result.IsSuccess) return Results.Ok(ApiResponse.SuccessResponse("Embedding refreshed."));
            return result.Errors.ToProblem();
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<SpecialistProfileResponse>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyProfile(CancellationToken ct)
        {
            var result = await specialistService.GetMyProfileAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistProfileResponse>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<SpecialistProfileResponse>), StatusCodes.Status200OK)]
        public async Task<IResult> Update([FromBody] UpdateSpecialistRequest request, CancellationToken ct)
        {
            var result = await specialistService.UpdateAsync(request, ct);

            return result.Match(value => Results.Ok(
                    ApiResponse<SpecialistProfileResponse>
                        .SuccessResponse(value, "Profile updated successfully.")),
                errors => errors.ToProblem());
        }

        #endregion


        #region Availability

        [HttpGet("me/availability")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AvailabilityResponse>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyAvailability(CancellationToken ct)
        {
            var result = await specialistService
                .GetMyAvailabilityAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<List<AvailabilityResponse>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("me/availability")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        public async Task<IResult> AddAvailabilities(AddAvailabilitiesRequest request, CancellationToken ct)
        {
            var result = await specialistService
                   .AddAvailabilitiesAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                   ApiResponse<IReadOnlyList<Guid>>.SuccessResponse(value,"Availability created successfully.")),
                            
    
                errors => errors.ToProblem());
        }

        [HttpDelete("me/availability/{id:guid}")]
        [Authorize]
        public async Task<IResult> DeleteAvailability(Guid id, CancellationToken ct)
        {
            var result = await specialistService.DeleteAvailabilityAsync(id, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Availability deleted successfully."));

            return result.Errors.ToProblem();
        }

        #endregion

        #region Expertise

        [HttpPost("me/expertise")]
        [Authorize]
        public async Task<IResult> AddExpertise([FromBody] AddExpertiseRequest request, CancellationToken ct)
        {
            var result = await specialistService
                    .AddExpertiseAsync(request, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Expertise added successfully."));

            return result.Errors.ToProblem();
        }

        [HttpDelete("me/expertise/{id:guid}")]
        [Authorize]
        public async Task<IResult> DeleteExpertise(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                    .DeleteExpertiseAsync(id, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Expertise deleted successfully."));

            return result.Errors.ToProblem();
        }

        [HttpGet("me/expertise")]
        [Authorize]
        public async Task<IResult> GetMyExpertise(CancellationToken ct)
        {
            var result = await specialistService.GetMyExpertiseAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<ExpertiseResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        #endregion

        #region earnings

        [HttpGet("me/earnings")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IActionResult> GetMyEarnings()
        {
            var result = await specialistService.GetEarningsAsync(HttpContext.RequestAborted);

            if (result.IsError)
            {
                return BadRequest(result.Errors);
            }
            return Ok(result.Value);
        }

        #endregion


        [HttpPut("me/cancellation-policy")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateCancellationPolicy(
            [FromBody] UpdateCancellationPolicyRequest request,
            CancellationToken ct)
        {
            var result = await specialistService
                .UpdateCancellationPolicyAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<bool>.SuccessResponse(
                        value,
                        "Cancellation policy updated successfully.")),
                errors => errors.ToProblem());
        }


        [HttpPut("me/booking-policy")]

        public async Task<IResult> UpdateBookingPolicy(

       UpdateBookingPolicyRequest request,

       CancellationToken ct)

        {

            var result = await specialistService.UpdateBookingPolicyAsync(request, ct);

            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value)),
                errors => errors.ToProblem());

        }



        [HttpGet("me/experience")]
        public async Task<IResult> GetExperience(CancellationToken ct)
        {
            var result = await specialistService
                .GetExperienceAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<ExperienceDto>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }



        [HttpPost("me/experience")]
        public async Task<IResult> AddExperiences(AddExperiencesRequest request, CancellationToken ct)
        {
            var result =
                await specialistService
                    .AddExperiencesAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<Guid>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }


        [HttpPut("me/experience/{id:guid}")]
        public async Task<IResult> UpdateExperience(Guid id, UpdateExperienceRequest request, CancellationToken ct)
        {
            var result =
                await specialistService.UpdateExperienceAsync(id, request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<bool>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }


        [HttpDelete("me/experience/{id:guid}")]
        public async Task<IResult> DeleteExperience(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                .DeleteExperienceAsync(id, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse(
                        "Experience deleted successfully."));
            }

            return result.Errors.ToProblem();
        }


        [HttpPost("me/skills")]
        public async Task<IResult> AddSkills(AddSkillRequest request, CancellationToken ct)
        {
            var result = await specialistService
                .AddSkillsAsync(request, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse(
                        "Skill added successfully."));
            }

            return result.Errors.ToProblem();
        }



        [HttpDelete("me/skills/{id:guid}")]
        public async Task<IResult> DeleteSkill(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                .DeleteSkillAsync(id, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse(
                        "Skill deleted successfully."));
            }

            return result.Errors.ToProblem();
        }


        [HttpPost("me/tools")]
        public async Task<IResult> AddTool(AddToolRequest request, CancellationToken ct)
        {
            var result = await specialistService
                .AddToolsAsync(request, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse(
                        "Tool added successfully."));
            }

            return result.Errors.ToProblem();
        }


        [HttpDelete("me/tools/{id:guid}")]
        public async Task<IResult> DeleteTool(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                .DeleteToolAsync(id, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse(
                        "Tool deleted successfully."));
            }

            return result.Errors.ToProblem();
        }



        [HttpGet]
        [AllowAnonymous]
        public async Task<IResult> GetSpecialists([FromQuery] GetSpecialistsRequest request, CancellationToken ct)
        {
            var result = await specialistService.GetSpecialistsAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<PaginatedResult<SpecialistListItemResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IResult> GetSpecialistById(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                .GetSpecialistByIdAsync(id, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistDetailsResponse>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }


        [HttpGet("{id:guid}/availability")]
        [AllowAnonymous]
        public async Task<IResult> GetSpecialistAvailability(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                .GetSpecialistAvailabilityAsync(id, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<SpecialistAvailabilityResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }


        [HttpGet("me/dashboard")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IResult> GetDashboard(CancellationToken cancellationToken)

        {
            var result = await specialistService.GetDashboardAsync(
                cancellationToken);

            return result.Match(
                value => Results.Ok(ApiResponse<SpecialistDashboardDto>
                    .SuccessResponse(value)),
                errors => errors.ToProblem());
        }


        [AllowAnonymous]
        [HttpGet("{id:guid}/certificates")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecialistDocumentResponse>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetCertificates(Guid id, CancellationToken ct)
        {
            var result = await specialistService.GetCertificatesAsync(id, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<SpecialistDocumentResponse>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("{id:guid}/reviews")]
        public async Task<IResult> GetReviews(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await specialistService.GetReviewsAsync(
                id,
                pageNumber,
                pageSize,
                ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistReviewsResponse>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("me/reviews")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IResult> GetMyReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await specialistService.GetMyReviewsAsync(
                pageNumber,
                pageSize,
                ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistReviewsResponse>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("me/skills")]
        public async Task<IResult> GetSkills(CancellationToken ct)
        {
            var result = await specialistService.GetSkillsAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<SkillResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("me/tools")]
        public async Task<IResult> GetTools(CancellationToken ct)
        {
            var result = await specialistService.GetToolsAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<ToolResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        #region Submission

        [HttpPost("me/submit")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IResult> SubmitForReview(CancellationToken ct)
        {
            var result = await specialistService.SubmitForReviewAsync(ct);
            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Profile submitted for review successfully."));
            return result.Errors.ToProblem();
        }

        #endregion

        #region Documents

        [HttpGet("me/documents")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecialistDocumentResponse>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetDocuments(CancellationToken ct)
        {
            var specialistId = await GetCurrentSpecialistIdAsync();
            if (specialistId == Guid.Empty)
                return Results.NotFound();

            var result = await specialistService.GetDocumentsAsync(specialistId, ct);
            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<SpecialistDocumentResponse>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("me/documents")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        public async Task<IResult> UploadDocument(
            IFormFile file,
            [FromQuery] SpecialistDocumentType type,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<Guid>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "No file uploaded.")]));

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(ApiResponse<Guid>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "File size must not exceed 10MB.")]));

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return Results.BadRequest(ApiResponse<Guid>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "Invalid file type. Allowed: JPG, JPEG, PNG, GIF, PDF.")]));

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "specialists", "documents");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{baseUrl}/uploads/specialists/documents/{uniqueFileName}";

            var specialistId = await GetCurrentSpecialistIdAsync();
            if (specialistId == Guid.Empty)
                return Results.NotFound();

            var docRequest = new UploadDocumentRequest
            {
                Type = type,
                Url = fileUrl,
                OriginalFileName = file.FileName
            };

            var result = await specialistService.UploadDocumentAsync(specialistId, docRequest, ct);
            return result.Match(
                value => Results.Ok(
                    ApiResponse<Guid>.SuccessResponse(value, "Document uploaded successfully.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("me/documents/{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IResult> DeleteDocument(Guid id, CancellationToken ct)
        {
            var specialistId = await GetCurrentSpecialistIdAsync();
            if (specialistId == Guid.Empty)
                return Results.NotFound();

            var result = await specialistService.DeleteDocumentAsync(specialistId, id, ct);
            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Document deleted successfully."));
            return result.Errors.ToProblem();
        }

        #endregion

        #region Verification

        [HttpGet("me/verification")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<VerificationDetailsResponse>), StatusCodes.Status200OK)]
        public async Task<IResult> GetVerification(CancellationToken ct)
        {
            var specialistId = await GetCurrentSpecialistIdAsync();
            if (specialistId == Guid.Empty)
                return Results.NotFound();

            var result = await specialistService.GetVerificationAsync(specialistId, ct);
            return result.Match(
                value => Results.Ok(
                    ApiResponse<VerificationDetailsResponse>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        //[HttpGet("me/verification/status")]
        //[Authorize]
        //[ProducesResponseType(typeof(ApiResponse<VerificationStatusResponse>), StatusCodes.Status200OK)]
        //public async Task<IResult> GetVerificationStatus(CancellationToken ct)
        //{
        //    var specialistId = await GetCurrentSpecialistIdAsync();
        //    if (specialistId == Guid.Empty)
        //        return Results.NotFound();

        //    var result = await specialistService.GetVerificationStatusAsync(specialistId, ct);
        //    return result.Match(
        //        value => Results.Ok(
        //            ApiResponse<VerificationStatusResponse>.SuccessResponse(value)),
        //        errors => errors.ToProblem());
        //}

        [HttpPost("me/verification")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IResult> SubmitVerification(CancellationToken ct)
        {
            var specialistId = await GetCurrentSpecialistIdAsync();
            if (specialistId == Guid.Empty)
                return Results.NotFound();

            var result = await specialistService.SubmitVerificationAsync(specialistId, ct);
            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Verification submitted successfully."));
            return result.Errors.ToProblem();
        }

        #endregion

        #region Portfolio

        [HttpGet("{specialistId:guid}/portfolio")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<PortfolioItemDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetPublicPortfolio(Guid specialistId, CancellationToken ct)
        {
            var result = await portfolioService.GetPublicAsync(specialistId, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<PortfolioItemDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("{specialistId:guid}/portfolio/{itemId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
        public async Task<IResult> GetPublicPortfolioItem(Guid specialistId, Guid itemId, CancellationToken ct)
        {
            var result = await portfolioService.GetByIdAsync(specialistId, itemId, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<PortfolioItemDto>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("me/portfolio")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<PortfolioItemDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyPortfolio(CancellationToken ct)
        {
            var result = await portfolioService.GetMyAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<PortfolioItemDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("me/portfolio/upload-image")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IResult> UploadPortfolioImage(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<string>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "No file uploaded.")]));

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(ApiResponse<string>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "File size must not exceed 10MB.")]));

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return Results.BadRequest(ApiResponse<string>.FailureResponse(
                    [BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "Invalid file type. Allowed: JPG, JPEG, PNG, GIF, WEBP.")]));

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "specialists", "portfolio");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{baseUrl}/uploads/specialists/portfolio/{uniqueFileName}";

            return Results.Ok(ApiResponse<string>.SuccessResponse(fileUrl, "Image uploaded."));
        }

        [HttpPost("me/portfolio")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
        public async Task<IResult> CreatePortfolioItem([FromBody] CreatePortfolioItemRequest request, CancellationToken ct)
        {
            var result = await portfolioService.CreateAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<PortfolioItemDto>.SuccessResponse(value, "Portfolio item created.")),
                errors => errors.ToProblem());
        }

        [HttpPut("me/portfolio/{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PortfolioItemDto>), StatusCodes.Status200OK)]
        public async Task<IResult> UpdatePortfolioItem(Guid id, [FromBody] UpdatePortfolioItemRequest request, CancellationToken ct)
        {
            var result = await portfolioService.UpdateAsync(id, request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<PortfolioItemDto>.SuccessResponse(value, "Portfolio item updated.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("me/portfolio/{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> DeletePortfolioItem(Guid id, CancellationToken ct)
        {
            var result = await portfolioService.DeleteAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Portfolio item deleted.")),
                errors => errors.ToProblem());
        }

        [HttpPut("me/portfolio/reorder")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> ReorderPortfolio([FromBody] ReorderPortfolioRequest request, CancellationToken ct)
        {
            var result = await portfolioService.ReorderAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Portfolio reordered.")),
                errors => errors.ToProblem());
        }

        #endregion
    }
}
//Khaled$123
//KH@gmail.com