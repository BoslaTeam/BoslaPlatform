using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Features.Specialists.Services;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared.Constants;
using BoslaPlatform.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/specialists")]
    [Authorize]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class SpecialistsController(ISpecialistService specialistService) : ControllerBase
    {
        #region Onboard & Profile 

        [HttpPost("onboard")]
        [ProducesResponseType(typeof(ApiResponse<SpecialistOnboardResponse>), StatusCodes.Status200OK)]
        public async Task<IResult> Onboard([FromBody] SpecialistOnboardRequest request, CancellationToken ct)
        {
            var result = await specialistService.OnboardAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistOnboardResponse>.SuccessResponse(
                        value, "Specialist onboarded successfully.")),
                errors => errors.ToProblem());
        }

        [HttpGet("me")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
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
        [Authorize(Roles = nameof(UserRole.Specialist))]
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
        [Authorize(Roles = nameof(UserRole.Specialist))]
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
        [Authorize(Roles = nameof(UserRole.Specialist))]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        public async Task<IResult> AddAvailabilities(AddAvailabilitiesRequest request, CancellationToken ct)
        {
            var result = await specialistService.AddAvailabilitiesAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<IReadOnlyList<Guid>>.SuccessResponse(value, "Availabilities created successfully.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("me/availability/{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
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
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IResult> AddExpertise([FromBody] AddExpertiseRequest request, CancellationToken ct)
        {
            var result = await specialistService
                    .AddExpertiseAsync(request, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Expertise added successfully."));

            return result.Errors.ToProblem();
        }

        [HttpDelete("me/expertise/{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Specialist))]
        public async Task<IResult> DeleteExpertise(Guid id, CancellationToken ct)
        {
            var result = await specialistService
                    .DeleteExpertiseAsync(id, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Expertise deleted successfully."));

            return result.Errors.ToProblem();
        }

        #endregion

        #region earnings

        [HttpGet("me/earnings")]
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

    }
}
//Khaled$123
//KH@gmail.com