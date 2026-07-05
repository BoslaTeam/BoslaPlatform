using BoslaPlatform.Application.Features.Favorites.DTOs;
using BoslaPlatform.Application.Features.Favorites.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Favorites
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;

        public FavoriteService(IAppDbContext context, IUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        private Result<Guid> GetUserId()
        {
            if (_currentUser.Id == null)
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated.");
            return _currentUser.Id.Value;
        }

        public async Task<Result<List<FavoriteSpecialistDto>>> GetMyFavoritesAsync(CancellationToken ct = default)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return userIdResult.Errors;

            var userId = userIdResult.Value;

            var items = await _context.Set<FavoriteSpecialist>()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAtUtc)
                .Select(f => new
                {
                    f.Id,
                    f.SpecialistId,
                    f.CreatedAtUtc,
                    UserName = f.Specialist.User.Name,
                    UserTitle = f.Specialist.User.Title,
                    UserImage = f.Specialist.User.ProfileImageUrl,
                    AvgRating = f.Specialist.Reviews.Select(r => (double)r.Rating).DefaultIfEmpty(0).Average(),
                    IsVerified = f.Specialist.Verification != null && f.Specialist.Verification.Status == VerificationStatus.Approved,
                    ExperienceLevel = (int)f.Specialist.ExperienceLevel,
                    HourlyRate = f.Specialist.HourlyRate
                })
                .ToListAsync(ct);

            var dtos = items.Select(i => new FavoriteSpecialistDto(
                i.Id,
                i.SpecialistId,
                i.UserName,
                i.UserTitle ?? i.UserName,
                i.UserImage,
                i.AvgRating,
                i.IsVerified,
                i.ExperienceLevel,
                i.HourlyRate,
                i.CreatedAtUtc)).ToList();

            return Result<List<FavoriteSpecialistDto>>.Success(dtos);
        }

        public async Task<Result<bool>> ToggleFavoriteAsync(Guid specialistId, CancellationToken ct = default)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return userIdResult.Errors;

            var userId = userIdResult.Value;
            var existing = await _context.Set<FavoriteSpecialist>()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SpecialistId == specialistId, ct);

            if (existing != null)
            {
                _context.Set<FavoriteSpecialist>().Remove(existing);
                await _context.SaveChangesAsync(ct);
                return Result<bool>.Success(false);
            }

            var specialistExists = await _context.Set<Specialist>()
                .AnyAsync(s => s.Id == specialistId, ct);

            if (!specialistExists)
                return Error.NotFound("Specialist.NotFound", "Specialist not found.");

            var favorite = new FavoriteSpecialist
            {
                UserId = userId,
                SpecialistId = specialistId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _context.Set<FavoriteSpecialist>().Add(favorite);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> IsFavoritedAsync(Guid specialistId, CancellationToken ct = default)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return Result<bool>.Success(false);

            var exists = await _context.Set<FavoriteSpecialist>()
                .AnyAsync(f => f.UserId == userIdResult.Value && f.SpecialistId == specialistId, ct);

            return Result<bool>.Success(exists);
        }
    }
}
