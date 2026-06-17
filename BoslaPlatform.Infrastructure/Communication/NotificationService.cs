using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.Infrastructure.Communication
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IUser _currentUser;

        public NotificationService(AppDbContext context, IUser currentUser)
        {
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

        public async Task<Result<List<NotificationDto>>> GetMyAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync(ct);

            var dtos = notifications.Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type.ToString(),
                n.IsRead,
                n.CreatedAtUtc)).ToList();

            return Result<List<NotificationDto>>.Success(dtos);
        }

        public async Task<Result<bool>> MarkReadAsync(Guid id, CancellationToken ct = default)
        {
            var userId = GetUserId();
            var notification = await _context.Set<Notification>()
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

            if (notification == null)
                return Error.NotFound(description: "Notification not found.");

            notification.IsRead = true;
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> MarkAllReadAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(ct);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
    }
}
