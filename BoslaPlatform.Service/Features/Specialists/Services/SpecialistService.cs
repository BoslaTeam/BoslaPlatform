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
    }
}