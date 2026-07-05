using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Communication
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private static readonly Dictionary<NotificationType, string[]> TypeRoles = new()
        {
            { NotificationType.Message, new[] { "User", "Specialist", "Admin" } },
            { NotificationType.Booking, new[] { "User", "Specialist", "Admin" } },
            { NotificationType.Reminder, new[] { "User", "Specialist", "Admin" } },
            { NotificationType.SpecialistVerification, new[] { "Specialist" } },
            { NotificationType.Withdrawal, new[] { "Specialist" } },
            { NotificationType.PortfolioApproved, new[] { "Specialist" } },
            { NotificationType.PortfolioRejected, new[] { "Specialist" } },
            { NotificationType.PortfolioPendingReview, new[] { "Admin" } },
        };

        private static string NormalizeRole(string? role) => role switch
        {
            "Admin" => "Admin",
            "Specialist" => "Specialist",
            _ => "User",
        };

        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;

        public NotificationPreferenceService(IAppDbContext context, IUser currentUser)
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

        private bool IsTypeForRole(NotificationType type, string role)
        {
            return TypeRoles.TryGetValue(type, out var roles) && roles.Contains(role);
        }

        public async Task<Result<List<NotificationPreferenceDto>>> GetMyAsync(CancellationToken ct = default)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return userIdResult.Errors;

            var userId = userIdResult.Value;
            var userRole = NormalizeRole(_currentUser.Role);
            var prefs = await _context.Set<UserNotificationPreference>()
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

            if (prefs.Count == 0)
            {
                await SeedDefaultsForRoleAsync(userId, userRole, ct);
                prefs = await _context.Set<UserNotificationPreference>()
                    .Where(p => p.UserId == userId)
                    .ToListAsync(ct);
            }

            var dtos = prefs
                .Where(p => IsTypeForRole(p.Type, userRole))
                .Select(p => new NotificationPreferenceDto(p.Type.ToString(), p.Enabled))
                .ToList();

            return Result<List<NotificationPreferenceDto>>.Success(dtos);
        }

        public async Task<Result<bool>> UpdateAsync(NotificationType type, bool enabled, CancellationToken ct = default)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return userIdResult.Errors;

            var userId = userIdResult.Value;
            var pref = await _context.Set<UserNotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, ct);

            if (pref == null)
            {
                pref = new UserNotificationPreference
                {
                    UserId = userId,
                    Type = type,
                    Enabled = enabled,
                };
                _context.Set<UserNotificationPreference>().Add(pref);
            }
            else
            {
                pref.Enabled = enabled;
            }

            await _context.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> GetMyByUserAsync(Guid userId, NotificationType type, CancellationToken ct = default)
        {
            var pref = await _context.Set<UserNotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, ct);

            if (pref == null)
                return Result<bool>.Success(true);

            return Result<bool>.Success(pref.Enabled);
        }

        public async Task SeedDefaultsAsync(Guid userId, string? role = null, CancellationToken ct = default)
        {
            var userRole = role != null ? NormalizeRole(role) : NormalizeRole(_currentUser.Role);
            await SeedDefaultsForRoleAsync(userId, userRole, ct);
        }

        private async Task SeedDefaultsForRoleAsync(Guid userId, string role, CancellationToken ct = default)
        {
            var existingTypes = await _context.Set<UserNotificationPreference>()
                .Where(p => p.UserId == userId)
                .Select(p => p.Type)
                .ToListAsync(ct);

            var allTypes = Enum.GetValues<NotificationType>().Cast<NotificationType>();
            var relevantTypes = allTypes.Where(t => IsTypeForRole(t, role));
            var missing = relevantTypes.Except(existingTypes).ToList();

            foreach (var type in missing)
            {
                _context.Set<UserNotificationPreference>().Add(
                    new UserNotificationPreference
                    {
                        UserId = userId,
                        Type = type,
                        Enabled = true,
                    });
            }

            if (missing.Count > 0)
                await _context.SaveChangesAsync(ct);
        }
    }
}
