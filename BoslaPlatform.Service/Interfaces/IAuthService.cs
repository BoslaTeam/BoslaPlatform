using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces
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
