using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WithdrawalCompletedEventHandler(
    IAppDbContext context,
    INotificationService notificationService,
    IEmailService emailService)
    : INotificationHandler<WithdrawalCompletedEvent>
{
    public async Task Handle(WithdrawalCompletedEvent notification, CancellationToken ct)
    {
        var specialist = await context.Specialists
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

        if (specialist is null) return;

        await notificationService.CreateAndSendNotificationAsync(
            specialist.UserId,
            "تم اكتمال السحب",
            $"تم اكتمال عملية السحب بمبلغ {notification.Amount:C}. تم تحويل المبلغ إلى حسابك البنكي.",
            NotificationType.Withdrawal,
            ct);

        if (!string.IsNullOrEmpty(specialist.User.Email))
        {
            await emailService.SendEmailAsync(
                specialist.User.Email,
                "تم اكتمال عملية السحب - بوصلة",
                $@"
                <h2>مرحباً {specialist.User.Name}</h2>
                <p>تم اكتمال عملية السحب بنجاح.</p>
                <ul>
                    <li>المبلغ: {notification.Amount:C}</li>
                    <li>تاريخ الاكتمال: {DateTime.UtcNow:yyyy-MM-dd}</li>
                </ul>
                <p>شكراً لاستخدامك بوصلة.</p>");
        }
    }
}
