using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WithdrawalRequestedEventHandler(
    IAppDbContext context,
    INotificationService notificationService,
    ILogger<WithdrawalRequestedEventHandler> logger)
    : INotificationHandler<WithdrawalRequestedEvent>
{
    public async Task Handle(WithdrawalRequestedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Withdrawal {Id} requested by specialist {SpecialistId}, amount {Amount}",
            notification.WithdrawalId, notification.SpecialistId, notification.Amount);

        var specialist = await context.Specialists
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

        if (specialist is null) return;

        await notificationService.CreateAndSendNotificationAsync(
            specialist.UserId,
            "تم تقديم طلب سحب",
            $"تم تقديم طلب سحب بمبلغ {notification.Amount:C}. قيد انتظار مراجعة الإدارة.",
            NotificationType.Withdrawal,
            ct,
            appointmentId: null);
    }
}
