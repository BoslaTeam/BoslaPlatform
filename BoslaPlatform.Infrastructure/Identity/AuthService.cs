using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Events.Identity;
using BoslaPlatform.Shared;
using BoslaPlatform.Shared.Constants;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly IPublisher _publisher;
        private readonly GoogleSettings _googleSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ITokenService tokenService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<User> signInManager,
            IUser currentUser,
            IEmailService emailService,
            IPublisher publisher,
            IOptions<GoogleSettings> googleSettings,
            ILogger<AuthService> logger)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _currentUser = currentUser;
            _emailService = emailService;
            _publisher = publisher;
            _googleSettings = googleSettings.Value;
            _logger = logger;
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

        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
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

            // Generate email confirmation token and send confirmation email
            await SendConfirmationEmailAsync(user);

            return Result<RegisterResponse>.Success(new RegisterResponse
            {
                Message = "Registration successful. Please check your email to confirm your account.",
                Email = user.Email!
            });
        }

        public async Task<Result<bool>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Error.NotFound(
                    description: "Invalid confirmation request.");
            }

            if (user.EmailConfirmed)
            {
                return Error.Validation(
                    description: "Email is already confirmed.");
            }

            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return result.Errors
                    .Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description))
                    .ToList();
            }

            // Publish EmailVerifiedEvent to trigger welcome email
            await _publisher.Publish(
                new EmailVerifiedEvent(user.Id, user.Email!, user.Name), ct);

            _logger.LogInformation(
                "Email confirmed successfully for user {UserId} ({Email})",
                user.Id, user.Email);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ResendConfirmationEmailAsync(
            ResendConfirmationEmailRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Return success even if user doesn't exist to prevent email enumeration
                return Result<bool>.Success(true);
            }

            if (user.EmailConfirmed)
            {
                return Error.Validation(
                    description: "Email is already confirmed.");
            }

            await SendConfirmationEmailAsync(user);

            return Result<bool>.Success(true);
        }

        public async Task<Result<TokenResponse>> GoogleLoginAsync(
            GoogleLoginRequest request, CancellationToken ct = default)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_googleSettings.ClientId]
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Error.Unauthorized(
                    description: "Invalid Google token.");
            }

            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user != null)
            {
                // Existing user — ensure they are active
                if (!user.IsActive)
                {
                    return Error.Forbidden(
                        description: "Your account is disabled.");
                }

                // Ensure Google login is linked
                var logins = await _userManager.GetLoginsAsync(user);
                if (!logins.Any(l => l.LoginProvider == "Google"))
                {
                    var addLoginResult = await _userManager.AddLoginAsync(user,
                        new UserLoginInfo("Google", payload.Subject, "Google"));

                    if (!addLoginResult.Succeeded)
                    {
                        _logger.LogWarning(
                            "Failed to link Google login for user {UserId}", user.Id);
                    }
                }

                // Mark email as confirmed since Google has verified it
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    
                    // Publish welcome event since the email is now verified
                    await _publisher.Publish(
                        new EmailVerifiedEvent(user.Id, user.Email!, user.Name), ct);
                }

                return await _tokenService.CreateTokenAsync(user, ct);
            }

            // New user — create account with Google info
            var newUser = new User
            {
                Email = payload.Email,
                UserName = payload.Email,
                Name = payload.Name ?? payload.Email,
                EmailConfirmed = true, // Google has already verified the email
                ProfileImageUrl = payload.Picture,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                return createResult.Errors
                    .Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description))
                    .ToList();
            }

            // Add default role
            await _userManager.AddToRoleAsync(newUser, Roles.User);

            // Link Google login
            await _userManager.AddLoginAsync(newUser,
                new UserLoginInfo("Google", payload.Subject, "Google"));

            _logger.LogInformation(
                "New user created via Google login: {UserId} ({Email})",
                newUser.Id, newUser.Email);

            // Publish welcome event for new Google users too
            await _publisher.Publish(
                new EmailVerifiedEvent(newUser.Id, newUser.Email!, newUser.Name), ct);

            return await _tokenService.CreateTokenAsync(newUser, ct);
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
                // Return success even if user doesn't exist to prevent email enumeration
                return Result<bool>.Success(true);
            }
            if (!user.IsActive)
            {
                // Return success to prevent account status enumeration
                return Result<bool>.Success(true);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            var resetLink = $"http://localhost:4200/auth/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
            
            var body = $@"
<div style='font-family: ""Inter"", ""Cairo"", Tahoma, Geneva, Verdana, sans-serif; background-color: #F7F9FA; padding: 40px 20px; text-align: center; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea;'>
        <div style='margin-bottom: 30px;'>
            <h1 style='color: #1B4F72; font-size: 28px; font-weight: 700; margin: 0;'>Bosla</h1>
        </div>
        <h2 style='color: #2C3E50; font-size: 22px; margin-bottom: 15px;'>Reset Your Password</h2>
        <p style='color: #2C3E50; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
            We received a request to reset the password for your account. Click the button below to choose a new password. If you didn't request this, you can safely ignore this email.
        </p>
        <a href='{resetLink}' style='display: inline-block; background-color: #F39C12; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 16px; font-weight: 600; border-radius: 6px; transition: background-color 0.3s;'>
            Reset Password
        </a>
        <p style='color: #95A5A6; font-size: 14px; margin-top: 35px; line-height: 1.5;'>
            Or copy and paste this link into your browser:<br>
            <a href='{resetLink}' style='color: #2E86AB; word-break: break-all; text-decoration: underline;'>{resetLink}</a>
        </p>
        <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;'>
        <p style='color: #95A5A6; font-size: 12px; margin: 0;'>
            &copy; {DateTime.UtcNow.Year} Bosla Platform. All rights reserved.<br>
            Your Compass to the Right Expert.
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

        #region Private Helpers

        private async Task SendConfirmationEmailAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmLink = $"http://localhost:4200/auth/verify-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            var body = $@"
<div style='font-family: ""Inter"", ""Cairo"", Tahoma, Geneva, Verdana, sans-serif; background-color: #F7F9FA; padding: 40px 20px; text-align: center; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea;'>
        <div style='margin-bottom: 30px;'>
            <h1 style='color: #1B4F72; font-size: 28px; font-weight: 700; margin: 0;'>Bosla</h1>
        </div>
        <h2 style='color: #2C3E50; font-size: 22px; margin-bottom: 15px;'>Verify Your Email Address</h2>
        <p style='color: #2C3E50; font-size: 16px; line-height: 1.6; margin-bottom: 10px;'>
            Hi <strong>{user.Name}</strong>,
        </p>
        <p style='color: #2C3E50; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
            Thank you for registering with Bosla! Please click the button below to verify your email address and activate your account.
        </p>
        <a href='{confirmLink}' style='display: inline-block; background-color: #F39C12; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 16px; font-weight: 600; border-radius: 6px;'>
            Verify Email
        </a>
        <p style='color: #95A5A6; font-size: 14px; margin-top: 35px; line-height: 1.5;'>
            Or copy and paste this link into your browser:<br>
            <a href='{confirmLink}' style='color: #2E86AB; word-break: break-all; text-decoration: underline;'>{confirmLink}</a>
        </p>
        <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;'>
        <p style='color: #95A5A6; font-size: 12px; margin: 0;'>
            If you didn't create an account, you can safely ignore this email.
        </p>
        <p style='color: #95A5A6; font-size: 12px; margin: 5px 0 0;'>
            &copy; {DateTime.UtcNow.Year} Bosla Platform. All rights reserved.<br>
            Your Compass to the Right Expert.
        </p>
    </div>
</div>";

            await _emailService.SendEmailAsync(
                user.Email!,
                "Verify Your Email - Bosla Platform",
                body);

            _logger.LogInformation(
                "Confirmation email sent to {Email} for user {UserId}",
                user.Email, user.Id);
        }

        #endregion
    }

}
