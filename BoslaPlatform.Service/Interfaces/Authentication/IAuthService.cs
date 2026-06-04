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
    }
}
