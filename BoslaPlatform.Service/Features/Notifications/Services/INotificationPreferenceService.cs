using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.Notifications.Services
{
    public interface INotificationPreferenceService
    {
        Task<Result<List<NotificationPreferenceDto>>> GetMyAsync(CancellationToken ct = default);
        Task<Result<bool>> UpdateAsync(NotificationType type, bool enabled, CancellationToken ct = default);
        Task SeedDefaultsAsync(Guid userId, string? role = null, CancellationToken ct = default);
        Task<Result<bool>> GetMyByUserAsync(Guid userId, NotificationType type, CancellationToken ct = default);
    }
}
