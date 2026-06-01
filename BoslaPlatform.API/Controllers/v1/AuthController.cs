using Asp.Versioning;
using BoslaPlatform.API.Extensions;
using BoslaPlatform.Application.Interfaces;
using BoslaPlatform.Shared.Dtos.Auth;
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

    }

}
