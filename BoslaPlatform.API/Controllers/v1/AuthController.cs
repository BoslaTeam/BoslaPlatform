using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _authService
                .LoginAsync(request, ct);

            return result.Match(
                value => Results.Ok(
                    ApiResponse<TokenResponse>
                        .SuccessResponse(value)),
                errors => errors.ToProblem());
        }
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var result = await _authService
                .RegisterAsync(request, ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<TokenResponse>
                            .SuccessResponse(
                                value,
                                "User registered successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<TokenResponse>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> Logout(CancellationToken ct)
        {
            var result = await _authService.LogoutAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Logged out successfully.")),
                errors => errors.ToProblem());
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            var result = await _authService.ForgotPasswordAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "If an account exists, a reset token has been sent.")),
                errors => errors.ToProblem());
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _authService.ResetPasswordAsync(request, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Password reset successfully.")),
                errors => errors.ToProblem());
        }
    }

}
