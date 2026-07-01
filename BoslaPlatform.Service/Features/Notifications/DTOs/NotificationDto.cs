using System;

namespace BoslaPlatform.Application.Features.Notifications.DTOs
{
    public sealed record NotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Type,
        bool IsRead,
        DateTimeOffset CreatedAtUtc,
        Guid? AppointmentId = null,
        int? AppointmentStatus = null);
}
