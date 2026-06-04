using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;

namespace BoslaPlatform.Infrastructure.Identity
{
    public class TokenService : ITokenService
    {
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly IAppDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly TimeProvider _timeProvider;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IAppDbContext dbContext, UserManager<User> userManager,
            TimeProvider timeProvider)
        {
            _jwtSettings = jwtSettings;
            _dbContext = dbContext;
            _userManager = userManager;
            _timeProvider = timeProvider;
        }
        public async Task<Result<TokenResponse>> CreateTokenAsync(User user, CancellationToken ct = default)
        {

            var now = _timeProvider.GetUtcNow();
            var accessTokenExpires = now.AddMinutes(_jwtSettings.Value.TokenExpirationInMinutes);

            var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()!),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()!),
            new (JwtRegisteredClaimNames.Email, user.Email!),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new(ClaimTypes.Role, role));
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = accessTokenExpires.UtcDateTime,
                Issuer = _jwtSettings.Value.Issuer,
                Audience = _jwtSettings.Value.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.SecretKey)),
                    SecurityAlgorithms.HmacSha256Signature),
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));

            // Revoke old refresh tokens
            var activeTokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAt == null & rt.ExpiresOnUtc > DateTime.UtcNow).ToListAsync(ct);

            foreach (var old in activeTokens)
            {
                old.Revoke();
            }
            // Create new refresh token
            var rawRefreshToken = GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = HashToken(rawRefreshToken),
                ExpiresOnUtc = now.AddDays(_jwtSettings.Value.RefreshTokenExpirationInDays),
                CreatedAtUtc = now.UtcDateTime,
                CreatedByIp = string.Empty
            };
            await _dbContext.RefreshTokens.AddAsync(refreshToken, ct);
            await _dbContext.SaveChangesAsync();

            var tokenResponse = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                ExpiresOnUtc = accessTokenExpires.UtcDateTime
            };

            return tokenResponse;
        }

        public Result<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Value.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Value.Audience,
                ValidateLifetime = true
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out var validatedToken);
                // Ensure the token is a JWT and uses the expected signing algorithm
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    return Error.Unauthorized(description: "Invalid token.");
                }
                // Check the signing algorithm
                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.OrdinalIgnoreCase))
                {
                    return Error.Unauthorized(description: "Invalid token algorithm.");
                }
                return principal;
            }
            catch (SecurityTokenException)
            {
                return Error.Unauthorized(description: "Invalid or expired token.");
            }
            catch (Exception)
            {
                return Error.Unexpected(
                    description: "Unexpected token validation error.");
            }
        }

        public async Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            var principalResult = GetPrincipalFromExpiredToken(request.AccessToken);

            if (principalResult.IsError)
            {
                return principalResult.Errors;
            }

            var userIdClaim = principalResult.Value.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Error.Unauthorized(
                    description: "Invalid token claims.");
            }

            var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
                return Error.NotFound("User not found");

            // Hash the provided refresh token to compare with stored hash
            var hashedRefreshToken = HashToken(request.RefreshToken);

            // Fetch all active tokens for the user
            var storedToken = await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.UserId == userId
                && rt.Token == hashedRefreshToken, ct);

            if (storedToken is null || !storedToken.IsActive)
                return Error.Unauthorized(description: "Invalid or expired refresh token.");

            // Revoke old token
            storedToken.Revoke();

            await _dbContext.SaveChangesAsync(ct);

            return await CreateTokenAsync(user, ct);
        }

        public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var hashedRefreshToken = HashToken(refreshToken);

            var storedToken = await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.Token == hashedRefreshToken, ct);

            if (storedToken is null)
                return Result.Failure(Error.NotFound("Refresh token not found"));

            if (!storedToken.IsActive)
                return Result.Failure(Error.NotFound("Token already revoked or expired."));


            storedToken.Revoke();
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();


        }
        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
        private static string HashToken(string token) { var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token)); return Convert.ToHexString(bytes); }
    }

}
