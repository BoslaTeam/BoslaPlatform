using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Apoointments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Appointments
{
    public sealed class AppointmentStatusChangedEventHandler
        : INotificationHandler<AppointmentStatusChangedEvent>
    {
        private readonly IAppDbContext _context;
        private readonly INotificationService _notificationService;

        public AppointmentStatusChangedEventHandler(
            IAppDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task Handle(
            AppointmentStatusChangedEvent notification,
            CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == notification.AppointmentId, ct);

            if (appointment is null) return;

            var otherPartyId = notification.ChangedByUserId == appointment.SpecialistId
                ? appointment.UserId
                : appointment.SpecialistId;

            var (title, message) = notification.NewStatus switch
            {
                AppointmentStatus.Confirmed => (
                    "تم تأكيد الحجز",
                    "تم تأكيد حجز الاستشارة بنجاح. يمكنك الآن البدء في الاستعداد للجلسة."),
                AppointmentStatus.Cancelled => (
                    "تم إلغاء الحجز",
                    $"تم إلغاء الحجز. السبب: {notification.Reason ?? "غير محدد"}."),
                AppointmentStatus.Rescheduled => (
                    "تم إعادة جدولة الحجز",
                    $"تم إعادة جدولة الموعد إلى وقت آخر. السبب: {notification.Reason ?? "غير محدد"}."),
                AppointmentStatus.Completed => (
                    "اكتملت الجلسة",
                    "تم اكتمال جلسة الاستشارة بنجاح. شكراً لك."),
                _ => (null, null),
            };

            if (title is null) return;

            await _notificationService.CreateAndSendNotificationAsync(
                otherPartyId,
                title,
                message,
                NotificationType.Booking,
                ct);
        }
    }
}