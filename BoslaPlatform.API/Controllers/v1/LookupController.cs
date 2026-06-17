using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Lookup.Response;
using BoslaPlatform.Application.Interfaces.Lookup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/lookup")]
    public class LookupController(ILookupService lookupService) : ControllerBase
    {
        [HttpGet("expertise")]
        [OutputCache(Duration = 3600)]
        public async Task<IResult> GetExpertise(CancellationToken ct)
        {
            var result = await lookupService.GetExpertiseAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<List<LookupItemResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("industries")]
        [OutputCache(Duration = 3600)]
        public async Task<IResult> GetIndustries(CancellationToken ct)
        {
            var result = await lookupService.GetIndustriesAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<List<LookupItemResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("skills")]
        [OutputCache(Duration = 3600)]
        public async Task<IResult> GetSkills(CancellationToken ct)
        {
            var result = await lookupService.GetSkillsAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<List<LookupItemResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("tools")]
        [OutputCache(Duration = 3600)]
        public async Task<IResult> GetTools(CancellationToken ct)
        {
            var result = await lookupService.GetToolsAsync(ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<List<LookupItemResponse>>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }
    }
}
