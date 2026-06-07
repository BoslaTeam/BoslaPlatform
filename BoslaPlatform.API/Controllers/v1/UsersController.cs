using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Users.DTOs;
using BoslaPlatform.Application.Features.Users.Requests;
using BoslaPlatform.Application.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyProfile(CancellationToken ct)
        {
            var result = await _userService.GetMyProfileAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<UserProfileDto>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            var result = await _userService.UpdateProfileAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<UserProfileDto>.SuccessResponse(value, "Profile updated successfully.")),
                errors => errors.ToProblem());
        }

        [HttpPut("me/password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var result = await _userService.ChangePasswordAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Password changed successfully.")),
                errors => errors.ToProblem());
        }

        [HttpGet("me/education")]
        [ProducesResponseType(typeof(ApiResponse<List<EducationDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetEducation(CancellationToken ct)
        {
            var result = await _userService.GetEducationAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<EducationDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("me/education")]
        [ProducesResponseType(typeof(ApiResponse<EducationDto>), StatusCodes.Status200OK)]
        public async Task<IResult> AddEducation([FromBody] AddEducationRequest request, CancellationToken ct)
        {
            var result = await _userService.AddEducationAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<EducationDto>.SuccessResponse(value, "Education added successfully.")),
                errors => errors.ToProblem());
        }

        [HttpPut("me/education/{id}")]
        [ProducesResponseType(typeof(ApiResponse<EducationDto>), StatusCodes.Status200OK)]
        public async Task<IResult> UpdateEducation(Guid id, [FromBody] UpdateEducationRequest request, CancellationToken ct)
        {
            var result = await _userService.UpdateEducationAsync(id, request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<EducationDto>.SuccessResponse(value, "Education updated successfully.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("me/education/{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> DeleteEducation(Guid id, CancellationToken ct)
        {
            var result = await _userService.DeleteEducationAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Education deleted successfully.")),
                errors => errors.ToProblem());
        }

        [HttpGet("me/social-links")]
        [ProducesResponseType(typeof(ApiResponse<List<SocialLinkDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetSocialLinks(CancellationToken ct)
        {
            var result = await _userService.GetSocialLinksAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<SocialLinkDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("me/social-links")]
        [ProducesResponseType(typeof(ApiResponse<SocialLinkDto>), StatusCodes.Status200OK)]
        public async Task<IResult> AddSocialLink([FromBody] AddSocialLinkRequest request, CancellationToken ct)
        {
            var result = await _userService.AddSocialLinkAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<SocialLinkDto>.SuccessResponse(value, "Social link added successfully.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("me/social-links/{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> DeleteSocialLink(Guid id, CancellationToken ct)
        {
            var result = await _userService.DeleteSocialLinkAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Social link deleted successfully.")),
                errors => errors.ToProblem());
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _userService.GetByIdAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<UserProfileDto>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }
    }
}
