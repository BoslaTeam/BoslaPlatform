using BoslaPlatform.Application;
using BoslaPlatform.Application.Interfaces;
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

        public AuthService(
            ITokenService tokenService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<User> signInManager)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
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
    }

}
