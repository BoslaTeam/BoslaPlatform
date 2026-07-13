using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Appointments
{
    public sealed class AppointmentStatusChangedEventHandler
        : INotificationHandler<AppointmentStatusChangedEvent>
    {
        private readonly IAppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AppointmentStatusChangedEventHandler> _logger;

        public AppointmentStatusChangedEventHandler(
            IAppDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<AppointmentStatusChangedEventHandler> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(AppointmentStatusChangedEvent notification, CancellationToken ct)
        {
            _logger.LogInformation(
                "Handling appointment status change: AppointmentId={AppointmentId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
                notification.AppointmentId, notification.OldStatus, notification.NewStatus);

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Specialist)
                .FirstOrDefaultAsync(a => a.Id == notification.AppointmentId, ct);

            if (appointment is null)
            {
                _logger.LogWarning("Appointment {AppointmentId} not found", notification.AppointmentId);
                return;
            }

            _logger.LogInformation("Appointment loaded: UserEmail={Email}, UserName={Name}", appointment.User?.Email, appointment.User?.Name);

            // Pending -> Paid: notify client + specialist
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

            // Pending -> Confirmed: notify client + specialist + email + schedule payment deadline reminder
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

                // Email: confirmation + pending payment
                var confirmBody = BuildEmailTemplate(appointment.User.Name,
                    "تم تأكيد موعد الجلسة",
                    $@"<p>تم تأكيد موعد الجلسة من قبل المتخصص. لإتمام الحجز، يرجى استكمال الخطوات المطلوبة خلال المهلة المحددة.</p>",
                    appointment,
                    extraRows: $@"
                        <br>
                        <b>قيمة الجلسة:</b> {appointment.SessionPrice} ج.م");

                await SendEmailAsync(appointment.User.Email,
                    "تم تأكيد موعد الجلسة - في انتظار الدفع", confirmBody);

                // Schedule payment deadline reminder (5 hours after confirmation = 1 hour before 6h deadline)
                if (appointment.ConfirmedAt.HasValue)
                {
                    var paymentReminderTime = appointment.ConfirmedAt.Value.AddHours(5).UtcDateTime;
                    if (paymentReminderTime > DateTime.UtcNow)
                    {
                        _context.Set<Reminder>().Add(new Reminder
                        {
                            AppointmentId = appointment.Id,
                            UserId = appointment.UserId,
                            ReminderTime = paymentReminderTime,
                            IsSent = false,
                            Message = "تذكير: يتبقى ساعة واحدة فقط لانتهاء المهلة المسموحة للدفع. يرجى إتمام الدفع لتأكيد حجزك."
                        });
                        await _context.SaveChangesAsync(ct);
                    }
                }
            }

            // Confirmed -> Paid: notify specialist that user paid + email + backfill
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

                // Email: successful payment
                var paidBody = BuildEmailTemplate(appointment.User.Name,
                    "تم تأكيد الدفع بنجاح",
                    $@"<p>تم إتمام العملية بنجاح. موعد الجلسة مؤكد وفي الموعد المحدد.</p>",
                    appointment);

                await SendEmailAsync(appointment.User.Email,
                    "تم تأكيد الدفع - موعد جلستك مؤكد", paidBody);

                // Backfill all existing notifications for this appointment to Paid
                var existingNotifs = await _context.Set<Notification>()
                    .Where(n => n.AppointmentId == appointment.Id && n.AppointmentStatus != (int)AppointmentStatus.Paid)
                    .ToListAsync(ct);
                foreach (var n in existingNotifs)
                {
                    n.AppointmentStatus = (int)AppointmentStatus.Paid;
                }
            }

            // Schedule session reminders for user when paid (saves together with backfill)
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

            // Paid -> Confirmed: notify client
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

            // Paid/Confirmed -> Cancelled: notify + email + remove pending reminders
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

                var cancelBody = BuildEmailTemplate(appointment.User.Name,
                    "تم إلغاء الموعد",
                    "<p>تم إلغاء الموعد. يمكنك حجز موعد جديد في أي وقت.</p>",
                    null);

                await SendEmailAsync(appointment.User.Email,
                    "تم إلغاء الموعد واسترداد المبلغ", cancelBody);

                await RemovePendingRemindersAsync(appointment.Id, ct);
            }

            // Pending -> Cancelled: notify + email + remove reminders
            if (notification.OldStatus == AppointmentStatus.Pending && notification.NewStatus == AppointmentStatus.Cancelled)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    appointment.UserId,
                    "تم إلغاء الموعد",
                    "تم إلغاء الموعد من قبل المتخصص.",
                    NotificationType.Booking,
                    ct);

                var cancelBody = BuildEmailTemplate(appointment.User.Name,
                    "تم إلغاء الموعد",
                    "<p style='color: #2C3E50; font-size: 15px; line-height: 1.7;'>تم إلغاء الموعد من قبل المتخصص. يمكنك حجز موعد جديد في أي وقت.</p>",
                    null);

                await SendEmailAsync(appointment.User.Email,
                    "تم إلغاء الموعد", cancelBody);

                await RemovePendingRemindersAsync(appointment.Id, ct);
            }
        }

        private async Task RemovePendingRemindersAsync(Guid appointmentId, CancellationToken ct)
        {
            var pendingReminders = await _context.Set<Reminder>()
                .Where(r => r.AppointmentId == appointmentId && !r.IsSent)
                .ToListAsync(ct);

            if (pendingReminders.Count > 0)
            {
                _context.Set<Reminder>().RemoveRange(pendingReminders);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Removed {Count} pending reminders for appointment {AppointmentId}",
                    pendingReminders.Count, appointmentId);
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Email sent successfully to {Email} with subject '{Subject}'", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", toEmail, subject);
            }
        }

        private static string BuildEmailTemplate(string userName, string heading, string contentHtml,
            Appointment? appointment, string? extraRows = null)
        {
            var detailsHtml = "";
            if (appointment is not null)
            {
                detailsHtml = $@"
                        <br>
                        <b>تاريخ الموعد:</b> {appointment.Start:yyyy-MM-dd}<br>
                        <b>الوقت:</b> {appointment.Start:hh:mm tt} - {appointment.End:hh:mm tt}
                        {extraRows}";
            }

            return $@"
            <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #333;'>{heading}</h2>
                <p>مرحباً {userName}،</p>
                {contentHtml}
                {detailsHtml}
                <br>
                <hr>
                <p style='color: #999; font-size: 11px;'>Bosla Platform &copy; {DateTime.UtcNow.Year}</p>
            </div>";
        }
    }
}
