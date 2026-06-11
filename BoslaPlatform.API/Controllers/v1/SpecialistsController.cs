using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Interfaces.Specialists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/specialists")]
    [Authorize]
    public class SpecialistsController(
        ISpecialistService specialistService) : ControllerBase
    {
        [HttpPost("onboard")]
        [ProducesResponseType(typeof(ApiResponse<SpecialistOnboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IResult> Onboard([FromBody] SpecialistOnboardRequest request, CancellationToken ct)
        {
            var result = await specialistService.OnboardAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<SpecialistOnboardResponse>.SuccessResponse(
                        value,
                        "Specialist onboarded successfully.")),
                errors => errors.ToProblem());
        }
    }
}
