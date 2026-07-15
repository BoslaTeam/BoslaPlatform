using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payments;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payments;

public sealed class PaymentDisputeFiledEventHandler
    : INotificationHandler<PaymentDisputeFiledEvent>
{
    private readonly UserManager<User> _userManager;
    private readonly INotificationService _notificationService;

    public PaymentDisputeFiledEventHandler(
        UserManager<User> userManager,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task Handle(PaymentDisputeFiledEvent notification, CancellationToken ct)
    {
        await _notificationService.CreateAndSendNotificationAsync(
            notification.UserId,
            "تم استلام شكواك",
            "تم تقديم شكواك بنجاح. سيتم مراجعتها من قبل الإدارة في أقرب وقت.",
            NotificationType.Booking,
            ct,
            notification.AppointmentId);

        var admins = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Admin));

        foreach (var admin in admins)
        {
            await _notificationService.CreateAndSendNotificationAsync(
                admin.Id,
                "شكوى جديدة على معاملة مالية",
                $"قام المستخدم بتقديم شكوى جديدة: {notification.Reason}.",
                NotificationType.Booking,
                ct,
                notification.AppointmentId);
        }
    }
}
