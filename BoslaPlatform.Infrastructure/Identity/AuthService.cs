using BoslaPlatform.Application.Interfaces.Communication;
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
        private readonly IEmailService _emailService;

        public AuthService(
            ITokenService tokenService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<User> signInManager,
            IUser currentUser,
            IEmailService emailService)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _currentUser = currentUser;
            _emailService = emailService;
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
            if (user == null)
            {
                throw new Exception($"DEBUG: User not found for email: {request.Email}");
            }
            if (!user.IsActive)
            {
                throw new Exception($"DEBUG: User is not active for email: {request.Email}");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            var resetLink = $"https://localhost:44397/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
            
            var body = $@"
<div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; background-color: #f9f9fc; padding: 40px 20px; text-align: center;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea;'>
        <div style='margin-bottom: 30px;'>
            <h1 style='color: #2c3e50; font-size: 24px; margin: 0;'>Bosla Platform</h1>
        </div>
        <h2 style='color: #333333; font-size: 20px; margin-bottom: 15px;'>Reset Your Password</h2>
        <p style='color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
            We received a request to reset the password for your account. Click the button below to choose a new password. If you didn't request this, you can safely ignore this email.
        </p>
        <a href='{resetLink}' style='display: inline-block; background-color: #4361ee; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 16px; font-weight: 600; border-radius: 6px; transition: background-color 0.3s;'>
            Reset Password
        </a>
        <p style='color: #888888; font-size: 14px; margin-top: 35px; line-height: 1.5;'>
            Or copy and paste this link into your browser:<br>
            <a href='{resetLink}' style='color: #4361ee; word-break: break-all; text-decoration: underline;'>{resetLink}</a>
        </p>
        <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;'>
        <p style='color: #aaaaaa; font-size: 12px; margin: 0;'>
            &copy; {DateTime.UtcNow.Year} Bosla Platform. All rights reserved.
        </p>
    </div>
</div>";

            await _emailService.SendEmailAsync(user.Email!, "Reset Password - Bosla Platform", body);

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

            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            return Result<bool>.Success(true);
        }

    }

}
