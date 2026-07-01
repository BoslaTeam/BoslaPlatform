using System;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
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

        public async Task Handle(AppointmentStatusChangedEvent notification, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Specialist)
                .FirstOrDefaultAsync(a => a.Id == notification.AppointmentId, ct);

            if (appointment is null) return;

            // Pending → Paid: notify client + specialist
            if (notification.OldStatus == AppointmentStatus.Pending && notification.NewStatus == AppointmentStatus.Paid)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم اتمام عملية الدفع",
                    "تم دفع قيمة الجلسة بنجاح. سيتم إعلامك عند تأكيد الموعد.",
                    NotificationType.Booking,
                    ct,
                    appointmentId: appointment.Id);

                if (appointment.Specialist?.UserId != Guid.Empty)
                {
                    var specialistMessage = $"قام {appointment.User.Name} بدفع قيمة الجلسة. يمكنك الآن تأكيد الموعد.";
                    await _notificationService.CreateAndSendNotificationAsync(
                        appointment.Specialist!.UserId,
                        "تم اتمام عملية الدفع",
                        specialistMessage,
                        NotificationType.Booking,
                        ct);
                }
            }

            // Pending → Confirmed: notify client with payment link
            if (notification.OldStatus == AppointmentStatus.Pending && notification.NewStatus == AppointmentStatus.Confirmed)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم تأكيد الموعد",
                    "تم تأكيد الموعد من قبل المتخصص. يرجى إتمام الدفع لحضور الجلسة.",
                    NotificationType.Booking,
                    ct,
                    appointmentId: appointment.Id);

                if (appointment.Specialist?.UserId != Guid.Empty)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        appointment.Specialist!.UserId,
                        "تم تأكيد الموعد",
                        "تم تأكيد موعد الجلسة للعميل بنجاح.",
                        NotificationType.Booking,
                        ct);
                }
            }

            // Confirmed → Paid: notify specialist that user paid
            if (notification.OldStatus == AppointmentStatus.Confirmed && notification.NewStatus == AppointmentStatus.Paid)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم اتمام عملية الدفع",
                    "تم دفع قيمة الجلسة بنجاح. يمكنك حضور الجلسة في الموعد المحدد.",
                    NotificationType.Booking,
                    ct,
                    appointmentId: appointment.Id);

                if (appointment.Specialist?.UserId != Guid.Empty)
                {
                    var specialistMessage = $"قام {appointment.User.Name} بإتمام عملية الدفع.";
                    await _notificationService.CreateAndSendNotificationAsync(
                        appointment.Specialist!.UserId,
                        "تم اتمام عملية الدفع",
                        specialistMessage,
                        NotificationType.Booking,
                        ct);
                }

                // Backfill all existing notifications for this appointment to Paid
                var existingNotifs = await _context.Set<Notification>()
                    .Where(n => n.AppointmentId == appointment.Id && n.AppointmentStatus != (int)AppointmentStatus.Paid)
                    .ToListAsync(ct);
                foreach (var n in existingNotifs)
                {
                    n.AppointmentStatus = (int)AppointmentStatus.Paid;
                }
                await _context.SaveChangesAsync(ct);
            }

            // Schedule session reminders for user when paid
            if (notification.NewStatus == AppointmentStatus.Paid)
            {
                var now = DateTime.UtcNow;
                var start = appointment.Start.UtcDateTime;
                var joinWindow = start.AddMinutes(-15);

                var reminders = new[]
                {
                    new Reminder
                    {
                        AppointmentId = appointment.Id,
                        UserId = appointment.UserId,
                        ReminderTime = start.AddMinutes(-30) < now ? now : start.AddMinutes(-30),
                        IsSent = false,
                        Message = "موعد جلستك سيبدأ قريباً. يرجى التأهب للانضمام."
                    },
                    new Reminder
                    {
                        AppointmentId = appointment.Id,
                        UserId = appointment.UserId,
                        ReminderTime = joinWindow < now ? now : joinWindow,
                        IsSent = false,
                        Message = "يمكنك الآن الانضمام إلى الجلسة."
                    }
                };

                foreach (var r in reminders)
                {
                    _context.Set<Reminder>().Add(r);
                }

                await _context.SaveChangesAsync(ct);
            }

            // Paid → Confirmed: notify client
            if (notification.OldStatus == AppointmentStatus.Paid && notification.NewStatus == AppointmentStatus.Confirmed)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم تأكيد الموعد",
                    "تم تأكيد موعد الجلسة من قبل المتخصص. يمكنك حضور الجلسة في الموعد المحدد.",
                    NotificationType.Booking,
                    ct);

                if (appointment.Specialist?.UserId != Guid.Empty)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        appointment.Specialist!.UserId,
                        "تم تأكيد الموعد",
                        "تم تأكيد موعد الجلسة للعميل بنجاح.",
                        NotificationType.Booking,
                        ct);
                }
            }

            // Paid/Confirmed → Cancelled: notify client about refund
            if (notification.NewStatus == AppointmentStatus.Cancelled &&
                (notification.OldStatus == AppointmentStatus.Paid || notification.OldStatus == AppointmentStatus.Confirmed))
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم إلغاء الموعد واسترداد المبلغ",
                    "تم إلغاء الموعد وتم استرداد المبلغ بنجاح. يمكنك حجز موعد جديد في أي وقت.",
                    NotificationType.Booking,
                    ct);

                if (appointment.Specialist?.UserId != Guid.Empty)
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        appointment.Specialist!.UserId,
                        "تم إلغاء الموعد",
                        "تم إلغاء الموعد ورد المبلغ للعميل.",
                        NotificationType.Booking,
                        ct);
                }
            }

            // Pending → Cancelled: notify about cancellation only
            if (notification.OldStatus == AppointmentStatus.Pending && notification.NewStatus == AppointmentStatus.Cancelled)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم إلغاء الموعد",
                    "تم إلغاء الموعد من قبل المتخصص.",
                    NotificationType.Booking,
                    ct);
            }
        }
    }
}
