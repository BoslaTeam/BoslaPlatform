using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Features.Specialists.Services;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/specialists")]
    [Authorize]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class SpecialistsController(
        ISpecialistService specialistService) : ControllerBase
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
        public async Task<IResult> AddAvailability([FromBody] AddAvailabilityRequest request, CancellationToken ct)
        {
            var result = await specialistService
                    .AddAvailabilityAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<Guid>.SuccessResponse(value, "Availability created successfully.")),
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
        public async Task<IResult> DeleteExpertise(Guid id,CancellationToken ct)
        {
            var result = await specialistService
                    .DeleteExpertiseAsync(id, ct);

            if (result.IsSuccess)
                return Results.Ok(ApiResponse.SuccessResponse("Expertise deleted successfully."));

            return result.Errors.ToProblem();
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
        public async Task<IResult> AddExperience(AddExperienceRequestDTO request,CancellationToken ct)
        {
            var result =
                await specialistService
                    .AddExperienceAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<Guid>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }
    }
}
//Khaled$123
//    KH@gmail.com