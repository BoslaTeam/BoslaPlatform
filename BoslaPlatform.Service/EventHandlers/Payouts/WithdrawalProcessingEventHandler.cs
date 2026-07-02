using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WithdrawalProcessingEventHandler(
    IAppDbContext context,
    INotificationService notificationService)
    : INotificationHandler<WithdrawalProcessingEvent>
{
    public async Task Handle(WithdrawalProcessingEvent notification, CancellationToken ct)
    {
        var specialist = await context.Specialists
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

        if (specialist is null) return;

        await notificationService.CreateAndSendNotificationAsync(
            specialist.UserId,
            "طلب السحب قيد المعالجة",
            "تمت الموافقة على طلب السحب الخاص بك وهو الآن قيد المعالجة. سيتم تحويل المبلغ إلى حسابك خلال 5-7 أيام عمل.",
            NotificationType.Withdrawal,
            ct);
    }
}
