using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
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
        [ProducesResponseType(typeof(ApiResponse<SpecialistProfileResponse>),StatusCodes.Status200OK)]
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
        public async Task<IResult> AddAvailability( [FromBody] AddAvailabilityRequest request, CancellationToken ct)
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
            var result = await specialistService
                    .DeleteAvailabilityAsync(id, ct);

            if (result.IsSuccess)
            {
                return Results.Ok(
                    ApiResponse.SuccessResponse("Availability deleted successfully."));
            }

            return result.Errors.ToProblem();
        }
    }
}
