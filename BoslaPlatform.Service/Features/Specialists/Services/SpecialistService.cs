using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Application.Features.Specialists.Response;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
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
        #region Onboard & Profile Methods

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

        public async Task<Result<SpecialistProfileResponse>> GetMyProfileAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await context.Specialists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id.Value, ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            return MapToProfileDto(specialist);
        }

        public async Task<Result<SpecialistProfileResponse>> UpdateAsync(UpdateSpecialistRequest request, CancellationToken ct = default)
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

        #endregion

        #region Availability Methods

        public async Task<Result<List<AvailabilityResponse>>> GetMyAvailabilityAsync(CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(
                    description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            var availability = await context.AvailabilitySlots
                    .Where(x => x.SpecialistId == specialist.Id)
                    .OrderBy(x => x.Start)
                    .Select(x => new AvailabilityResponse(
                        x.Id,
                        x.Start,
                        x.End))
                    .ToListAsync(ct);

            return availability;
        }

        public async Task<Result<Guid>> AddAvailabilityAsync(AddAvailabilityRequest request, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialist = await GetCurrentSpecialistAsync(ct);

            if (specialist is null)
                return Error.NotFound(description: "Specialist profile not found.");

            if (request.Start.Offset != TimeSpan.Zero || request.End.Offset != TimeSpan.Zero)
                return Error.Validation(description: "Availability dates must be UTC.");

            var hasOverlap = await context.AvailabilitySlots
                    .AnyAsync(
                        x => x.SpecialistId == specialist.Id &&
                             request.Start < x.End &&
                             request.End > x.Start,
                        ct);

            if (hasOverlap)
                return Error.Conflict(description:"Availability slot overlaps with an existing slot.");

            var availability =
                Availability.Create(
                    specialist.Id,
                    request.Start,
                    request.End);

            await context.AvailabilitySlots
                .AddAsync(availability, ct);

            await context.SaveChangesAsync(ct);

            return availability.Id;
        }

        public async Task<Result> DeleteAvailabilityAsync(Guid availabilityId, CancellationToken ct = default)
        {
            if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            var specialistId = await context.Specialists
                .Where(x => x.UserId == currentUser.Id.Value)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (specialistId == Guid.Empty)
            {
                return Error.NotFound(description: "Specialist profile not found.");
            }

            var availability = await context.AvailabilitySlots
                    .FirstOrDefaultAsync(
                        x => x.Id == availabilityId &&
                             x.SpecialistId == specialistId,
                        ct);

            if (availability is null)
            {
                return Error.NotFound(
                        description: "Availability slot not found.");
            }

            context.AvailabilitySlots
                .Remove(availability);

            await context.SaveChangesAsync(ct);

            return Result.Success();
        }

        #endregion

        #region Helpers

        private async Task<Specialist?> GetCurrentSpecialistAsync(CancellationToken ct)
        {
            if (!currentUser.Id.HasValue)
                return null;

            return await context.Specialists
                .FirstOrDefaultAsync(
                    x => x.UserId == currentUser.Id.Value,
                    ct);
        }

        private Result<SpecialistProfileResponse> MapToProfileDto(Specialist specialist)
        {
            return new SpecialistProfileResponse(
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

        #endregion
    }
}