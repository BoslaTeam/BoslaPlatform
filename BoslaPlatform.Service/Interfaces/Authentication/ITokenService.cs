using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Shared;
using System.Security.Claims;

namespace BoslaPlatform.Application.Interfaces.Authentication
{
    public interface ITokenService
    {
        Task<Result<TokenResponse>> CreateTokenAsync(User user, CancellationToken ct = default);
        Result<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token);
        Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
        Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
        Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);

    }

}
