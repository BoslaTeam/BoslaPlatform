using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Dtos.Auth;
using System.Security.Claims;

namespace BoslaPlatform.Application.Interfaces
{
    public interface ITokenService
    {
        Task<Result<TokenResponse>> CreateTokenAsync(User user, CancellationToken ct = default);
        Result<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token);
        Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
        Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);

    }

}
