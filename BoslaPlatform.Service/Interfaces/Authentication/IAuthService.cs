using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Authentication
{
    public interface IAuthService
    {
        Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default);

        Task<Result<TokenResponse>> RegisterAsync(
            RegisterRequest request,
            CancellationToken ct = default);

        Task<Result<TokenResponse>> RefreshTokenAsync(
            RefreshTokenRequest request,
            CancellationToken ct = default);

        Task<Result<bool>> LogoutAsync(
            CancellationToken ct = default);

        Task<Result<bool>> ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken ct = default);

        Task<Result<bool>> ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken ct = default);
    }
}
