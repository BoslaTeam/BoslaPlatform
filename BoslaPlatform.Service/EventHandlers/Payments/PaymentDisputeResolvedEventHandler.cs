using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payments;

public sealed class PaymentDisputeResolvedEventHandler
    : INotificationHandler<PaymentDisputeResolvedEvent>
{
    private readonly IAppDbContext _context;
    private readonly INotificationService _notificationService;

    public PaymentDisputeResolvedEventHandler(
        IAppDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(PaymentDisputeResolvedEvent notification, CancellationToken ct)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Specialist)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == notification.AppointmentId, ct);

        if (appointment is null) return;

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == notification.PaymentId, ct);

        var amountText = payment is not null ? $"{payment.Amount} {payment.Currency}" : "";

        if (notification.WasRefunded)
        {
            await _notificationService.CreateAndSendNotificationAsync(
                appointment.UserId,
                "تم استرجاع المبلغ",
                $"تمت الموافقة على شكواك وتم استرجاع مبلغ {amountText}.",
                NotificationType.RefundProcessed,
                ct);

            await _notificationService.CreateAndSendNotificationAsync(
                appointment.Specialist.UserId,
                "تم استرجاع مبلغ الجلسة",
                $"تم استرجاع مبلغ {amountText} بعد مراجعة الشكوى.",
                NotificationType.RefundProcessed,
                ct);
        }
        else
        {
            await _notificationService.CreateAndSendNotificationAsync(
                appointment.UserId,
                "تم رفض الشكوى",
                "تم رفض شكواك بعد المراجعة. تم إعادة المبلغ إلى الحجز.",
                NotificationType.RefundProcessed,
                ct);

            await _notificationService.CreateAndSendNotificationAsync(
                appointment.Specialist.UserId,
                "رفض الشكوى",
                $"تم رفض الشكوى على معاملة بمبلغ {amountText}.",
                NotificationType.RefundProcessed,
                ct);
        }
    }
}
