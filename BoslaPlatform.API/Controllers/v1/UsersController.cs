using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Users.DTOs;
using BoslaPlatform.Application.Features.Users.Requests;
using BoslaPlatform.Application.Interfaces.Authentication;
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
        private readonly IWebHostEnvironment _env;

        public UsersController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
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

        [HttpPost("me/set-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> SetPassword([FromBody] SetPasswordRequest request, CancellationToken ct)
        {
            var result = await _userService.SetPasswordAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Password set successfully.")),
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

        [HttpPost("me/profile-picture")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(ApiResponse<string>.FailureResponse(new List<BoslaPlatform.Shared.Error> { BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "No file uploaded.") }));
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return Results.BadRequest(ApiResponse<string>.FailureResponse(new List<BoslaPlatform.Shared.Error> { BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "File size must not exceed 5MB.") }));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return Results.BadRequest(ApiResponse<string>.FailureResponse(new List<BoslaPlatform.Shared.Error> { BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "File", "Invalid file type. Only JPG, JPEG, PNG and GIF are allowed.") }));
            }

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "images", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Delete old profile picture if exists
            var currentProfile = await _userService.GetMyProfileAsync(ct);
            currentProfile.Match(
                profile =>
                {
                    if (!string.IsNullOrEmpty(profile.ProfileImageUrl))
                    {
                        try
                        {
                            // Extract filename from URL (remove query string if any)
                            var oldUrl = new Uri(profile.ProfileImageUrl);
                            var oldFileName = Path.GetFileName(oldUrl.AbsolutePath);
                            var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        catch
                        {
                            // Ignore errors when deleting old file
                        }
                    }
                    return true;
                },
                errors => false);

            // Save new file
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            // Add cache-busting query param so browsers always load the latest image
            var fileUrl = $"{baseUrl}/images/profiles/{uniqueFileName}?v={DateTime.UtcNow.Ticks}";

            var updateRequest = new UpdateProfileRequest(null, null, null, fileUrl, null, null, null, null);
            var result = await _userService.UpdateProfileAsync(updateRequest, ct);

            return result.Match(
                value => Results.Ok(ApiResponse<string>.SuccessResponse(fileUrl, "Profile picture uploaded successfully.")),
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
