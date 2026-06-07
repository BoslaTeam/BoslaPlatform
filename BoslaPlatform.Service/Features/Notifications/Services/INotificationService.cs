using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.Application.Features.Notifications.Services
{
    public interface INotificationService
    {
        Task<Result<List<NotificationDto>>> GetMyAsync(CancellationToken ct = default);
        Task<Result<bool>> MarkReadAsync(Guid id, CancellationToken ct = default);
        Task<Result<bool>> MarkAllReadAsync(CancellationToken ct = default);
    }
}
