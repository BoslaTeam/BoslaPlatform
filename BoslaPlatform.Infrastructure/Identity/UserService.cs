using BoslaPlatform.Application.Features.Users.DTOs;
using BoslaPlatform.Application.Features.Users.Requests;
using BoslaPlatform.Application.Features.Users.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Identity
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly IUser _currentUser;

        public UserService(UserManager<User> userManager, AppDbContext context, IUser currentUser)
        {
            _userManager = userManager;
            _context = context;
            _currentUser = currentUser;
        }

        private Guid GetUserId()
        {
            if (_currentUser.Id == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return _currentUser.Id.Value;
        }

        public async Task<Result<UserProfileDto>> GetMyProfileAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return Error.NotFound(description: "User not found.");

            var dto = new UserProfileDto(
                user.Id,
                user.Email ?? string.Empty,
                user.Name,
                user.Country ?? string.Empty,
                user.Gender ?? string.Empty,
                user.PreferredLanguage ?? string.Empty,
                user.IsActive);

            return Result<UserProfileDto>.Success(dto);
        }

        public async Task<Result<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return Error.NotFound(description: "User not found.");

            user.Name = request.Name;
            user.Country = request.Country;
            user.Gender = request.Gender;
            user.PreferredLanguage = request.PreferredLanguage;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }

            var dto = new UserProfileDto(
                user.Id,
                user.Email ?? string.Empty,
                user.Name,
                user.Country,
                user.Gender,
                user.PreferredLanguage,
                user.IsActive);

            return Result<UserProfileDto>.Success(dto);
        }

        public async Task<Result<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return Error.NotFound(description: "User not found.");

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<EducationDto>>> GetEducationAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var educations = await _context.Set<Education>()
                .Where(e => e.UserId == userId)
                .ToListAsync(ct);

            var dtos = educations.Select(e => new EducationDto(
                e.Id,
                e.FieldOfStudy,
                e.InstitutionName,
                e.StartDate.Year,
                e.EndDate?.Year)).ToList();

            return Result<List<EducationDto>>.Success(dtos);
        }

        public async Task<Result<EducationDto>> AddEducationAsync(AddEducationRequest request, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var education = new Education
            {
                UserId = userId,
                FieldOfStudy = request.Degree,
                InstitutionName = request.Institution,
                StartDate = new DateOnly(request.StartYear, 1, 1),
                EndDate = request.EndYear.HasValue ? new DateOnly(request.EndYear.Value, 1, 1) : null
            };

            _context.Set<Education>().Add(education);
            await _context.SaveChangesAsync(ct);

            var dto = new EducationDto(
                education.Id,
                education.FieldOfStudy,
                education.InstitutionName,
                education.StartDate.Year,
                education.EndDate?.Year);

            return Result<EducationDto>.Success(dto);
        }

        public async Task<Result<EducationDto>> UpdateEducationAsync(Guid id, UpdateEducationRequest request, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var education = await _context.Set<Education>()
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

            if (education == null)
                return Error.NotFound(description: "Education record not found.");

            education.FieldOfStudy = request.Degree;
            education.InstitutionName = request.Institution;
            education.StartDate = new DateOnly(request.StartYear, 1, 1);
            education.EndDate = request.EndYear.HasValue ? new DateOnly(request.EndYear.Value, 1, 1) : null;

            await _context.SaveChangesAsync(ct);

            var dto = new EducationDto(
                education.Id,
                education.FieldOfStudy,
                education.InstitutionName,
                education.StartDate.Year,
                education.EndDate?.Year);

            return Result<EducationDto>.Success(dto);
        }

        public async Task<Result<bool>> DeleteEducationAsync(Guid id, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var education = await _context.Set<Education>()
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

            if (education == null)
                return Error.NotFound(description: "Education record not found.");

            _context.Set<Education>().Remove(education);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<SocialLinkDto>>> GetSocialLinksAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var links = await _context.Set<SocialLink>()
                .Where(l => l.UserId == userId)
                .ToListAsync(ct);

            var dtos = links.Select(l => new SocialLinkDto(
                l.Id,
                l.Title,
                l.Url)).ToList();

            return Result<List<SocialLinkDto>>.Success(dtos);
        }

        public async Task<Result<SocialLinkDto>> AddSocialLinkAsync(AddSocialLinkRequest request, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var link = new SocialLink
            {
                UserId = userId,
                Title = request.Platform,
                Url = request.Url
            };

            _context.Set<SocialLink>().Add(link);
            await _context.SaveChangesAsync(ct);

            var dto = new SocialLinkDto(link.Id, link.Title, link.Url);
            return Result<SocialLinkDto>.Success(dto);
        }

        public async Task<Result<bool>> DeleteSocialLinkAsync(Guid id, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var link = await _context.Set<SocialLink>()
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

            if (link == null)
                return Error.NotFound(description: "Social link not found.");

            _context.Set<SocialLink>().Remove(link);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<UserProfileDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return Error.NotFound(description: "User not found.");

            var dto = new UserProfileDto(
                user.Id,
                user.Email ?? string.Empty,
                user.Name,
                user.Country ?? string.Empty,
                user.Gender ?? string.Empty,
                user.PreferredLanguage ?? string.Empty,
                user.IsActive);

            return Result<UserProfileDto>.Success(dto);
        }
    }
}
