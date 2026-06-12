using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Specialists.Services
{
    public class SpecialistService(
        IAppDbContext context,
        IUser currentUser,
        UserManager<User> userManager) : ISpecialistService
    {
        public async Task<Result<SpecialistOnboardResponse>> OnboardAsync(SpecialistOnboardRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
            {
                return Error.Unauthorized(description: "User is not authenticated.");
            }

            var userId = currentUser.Id.Value;

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error.NotFound(description: "User not found.");

            var alreadySpecialist = await context.Specialists
                .AnyAsync(x => x.UserId == userId, ct);

            if (alreadySpecialist)
                return Error.Conflict(description: "User is already a specialist.");

            var specialist = Specialist.Create(
                userId,
                request.ExperienceYears,
                request.ExperienceLevel,
                request.HourlyRate,
                request.BookingPolicy
            );

            await context.Specialists.AddAsync(specialist, ct);

            var addRoleResult = await userManager.AddToRoleAsync(user, nameof(UserRole.Specialist));
            if (!addRoleResult.Succeeded)
            {
                return addRoleResult.Errors
                    .Select(x => Error.Validation(x.Code, x.Description))
                    .ToList();
            }

            await context.SaveChangesAsync(ct);

            return Result<SpecialistOnboardResponse>.Success(
                new SpecialistOnboardResponse(specialist.Id, specialist.VerificationStatus)
            );
        }

        public async Task<Result<SpecialistProfileDto>> GetMyProfileAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(
                    description: "User is not authenticated.");

            var specialist = await context.Specialists
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id.Value,ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            return MapToProfileDto(specialist);
        }


        public async Task<Result<SpecialistProfileDto>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await context.Specialists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id.Value, ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");


            specialist.UpdateProfile(
                request.ExperienceYears,
                request.ExperienceLevel,
                request.HourlyRate,
                request.IntroVideoUrl,
                request.BookingPolicy);

            await context.SaveChangesAsync(ct);

            return MapToProfileDto(specialist);
        }


        // Helpr Method
        private Result<SpecialistProfileDto> MapToProfileDto(Specialist specialist)
        {
            return new SpecialistProfileDto(
                specialist.Id,
                specialist.UserId,
                specialist.User.Email ?? string.Empty,
                specialist.User.Name,
                specialist.User.Title,
                specialist.User.Bio,
                specialist.User.ProfileImageUrl,
                specialist.User.Country,
                specialist.User.Gender,
                specialist.User.PreferredLanguage,
                specialist.ExperienceYears,
                specialist.ExperienceLevel,
                specialist.HourlyRate,
                specialist.IntroVideoUrl,
                specialist.VerificationStatus,
                specialist.BookingPolicy,
                specialist.MinBookingNoticeHours,
                specialist.MaxSessionsPerDay,
                specialist.MaxSessionsPerWeek,
                specialist.CancellationDeadlineHours,
                specialist.CancellationFeePercent
            );
        }
    }
}