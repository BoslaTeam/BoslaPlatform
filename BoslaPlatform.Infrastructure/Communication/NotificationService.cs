using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Communication;
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
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Application.Interfaces.Persistence;

namespace BoslaPlatform.Infrastructure.Communication
{
    public class NotificationService : INotificationService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly INotificationSender _notificationSender;

        public NotificationService(
            IAppDbContext context,
            IUser currentUser, 
            INotificationSender notificationSender)
        {
            _context = context;
            _currentUser = currentUser;
            _notificationSender = notificationSender;
        }

        private Result<Guid> GetUserId()
        {
            if (_currentUser.Id == null)
            {
                return Error.Unauthorized("User.Unauthorized","User is not authenticated.");
            }
            return _currentUser.Id.Value;
        }

        public async Task<Result<List<NotificationDto>>> GetMyAsync(CancellationToken ct = default)
        {
            var userIdResult = GetUserId();

            if (userIdResult.IsError)
            {
                return userIdResult.Errors;
            }

            var userId = userIdResult.Value;
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
            var userIdResult = GetUserId();

            if (userIdResult.IsError)
            {
                return userIdResult.Errors;
            }

            var userId = userIdResult.Value;
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
            var userIdResult = GetUserId();

            if (userIdResult.IsError)
            {
                return userIdResult.Errors;
            }

            var userId = userIdResult.Value;
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

        public async Task<Result<bool>> CreateAndSendNotificationAsync(Guid userId, string title, string message, NotificationType type, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                return Error.Validation(
                    "Notification.InvalidUser",
                    "User id is invalid.");
            }
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            _context.Set<Notification>().Add(notification);
            await _context.SaveChangesAsync(ct);

            var dto = new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type.ToString(),
                notification.IsRead,
                notification.CreatedAtUtc);

            await _notificationSender.SendToUserAsync(userId, dto,ct);

            return Result<bool>.Success(true);
        }
    }
}
