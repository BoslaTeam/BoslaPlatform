using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WithdrawalRejectedEventHandler(
    IAppDbContext context,
    INotificationService notificationService,
    IEmailService emailService)
    : INotificationHandler<WithdrawalRejectedEvent>
{
    public async Task Handle(WithdrawalRejectedEvent notification, CancellationToken ct)
    {
        var specialist = await context.Specialists
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

        if (specialist is null) return;

        var reason = !string.IsNullOrEmpty(notification.AdminNotes)
            ? $"السبب: {notification.AdminNotes}"
            : "لم يتم تحديد سبب.";

        await notificationService.CreateAndSendNotificationAsync(
            specialist.UserId,
            "تم رفض طلب السحب",
            $"تم رفض طلب السحب الخاص بك. {reason}",
            NotificationType.Withdrawal,
            ct);

        if (!string.IsNullOrEmpty(specialist.User.Email))
        {
            await emailService.SendEmailAsync(
                specialist.User.Email,
                "تم رفض طلب السحب - بوصلة",
                $@"
                <h2>مرحباً {specialist.User.Name}</h2>
                <p>نأسف لإعلامك بأن طلب السحب الخاص بك قد تم رفضه.</p>
                <p>{reason}</p>
                <p>إذا كان لديك أي استفسار، يرجى التواصل مع فريق الدعم.</p>");
        }
    }
}
