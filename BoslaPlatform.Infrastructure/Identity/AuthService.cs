using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUser _currentUser;

        public AuthService(
            ITokenService tokenService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<User> signInManager,
            IUser currentUser)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _currentUser = currentUser;
        }
        public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return Error.Unauthorized(
                    description: "Invalid email or password.");


            if (!user.IsActive)
            {
                return Error.Forbidden(
                    description: "Your account is disabled.");
            }

            var passwordValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    request.Password);

            if (!passwordValid)
            {
                return Error.Unauthorized(
                    description: "Invalid email or password.");
            }

            //if (!user.EmailConfirmed)
            //{
            //    return Error.Forbidden(
            //        description: "Please confirm your email first.");
            //}
            if (_userManager.SupportsUserLockout &&
            await _userManager.IsLockedOutAsync(user))
            {
                return Error.Forbidden(
                    description: "Your account is locked.");
            }

            return await _tokenService
                .CreateTokenAsync(user, ct);
        }

        public async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var exists = await _userManager.Users
                        .AnyAsync(u => u.Email == request.Email, ct);

            if (exists)
            {
                return Error.Conflict(
                    description: "Email already exists.");
            }

            var user = new User
            {
                Email = request.Email,
                UserName = request.Email.Split("@")[0],
                Country = request.Country,
                Gender = request.Gender,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                PreferredLanguage = request.PreferredLanguage,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }

            var roleExists = await _roleManager.RoleExistsAsync(request.Role);

            if (!roleExists)
                return Error.NotFound(
                    description: $"Role '{request.Role}' does not exist.");


            await _userManager.AddToRoleAsync(user, request.Role);

            return await _tokenService.CreateTokenAsync(user, ct);
        }

        public async Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            return await _tokenService.RefreshTokenAsync(request, ct);
        }

        public async Task<Result<bool>> LogoutAsync(CancellationToken ct = default)
        {
            await _signInManager.SignOutAsync();

            // Revoke all active refresh tokens for the current user
            if (_currentUser.Id.HasValue)
            {
                await _tokenService.RevokeAllUserTokensAsync(_currentUser.Id.Value, ct);
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
                return Result<bool>.Success(true); // Don't reveal that the user does not exist

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: Send email with the token (IEmailService)
            
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Error.NotFound(description: "User not found.");

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            return Result<bool>.Success(true);
        }
    }

}
