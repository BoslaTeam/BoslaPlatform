using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Constants;
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

            //if (!user.EmailConfirmed)
            //{
            //    return Error.Forbidden(
            //        description: "Please confirm your email first.");
            //}

            var signInResult =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return Error.Forbidden(
                    description: "Your account is locked.");
            if (!signInResult.Succeeded)
            {
                return Error.Unauthorized(
                    description: "Invalid email or password.");
            }
            return await _tokenService
                .CreateTokenAsync(user, ct);
        }

        public async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var allowedRoles = new[]
                        {
                            Roles.User,
                            Roles.Specialist
                        };

            if (!allowedRoles.Contains(request.Role.ToLower()))
            {
                return Error.Validation(
                    "Role.Invalid",
                    "Invalid role selection.");
            }
            var roleExists = await _roleManager.RoleExistsAsync(request.Role);

            if (!roleExists)
                return Error.NotFound(
                    description: $"Role '{request.Role}' does not exist.");

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
                UserName = request.Email,
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

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return roleResult.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }


            return await _tokenService.CreateTokenAsync(user, ct);
        }

        public async Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request,
            CancellationToken ct = default)
        {
            return await _tokenService.RefreshTokenAsync(request, ct);
        }

        public async Task<Result<bool>> LogoutAsync(CancellationToken ct = default)
        {
            // Revoke all active refresh tokens for the current user
            if (!_currentUser.Id.HasValue)
            {
                return Result<bool>.Success(true);
                
            }
            await _tokenService.RevokeAllUserTokensAsync(_currentUser.Id.Value, ct);

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
            {
                return Error.Validation(
                        description: "Invalid reset request.");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            return Result<bool>.Success(true);
        }

    }

}
