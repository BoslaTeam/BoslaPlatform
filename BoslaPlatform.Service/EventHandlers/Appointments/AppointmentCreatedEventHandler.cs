using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Communication;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Appointments
{
    public sealed class AppointmentCreatedEventHandler
        : INotificationHandler<AppointmentScheduledEvent>
    {
        private readonly IAppDbContext _context;
        private readonly INotificationService _notificationService;

        public AppointmentCreatedEventHandler(
            IAppDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task Handle(
            AppointmentScheduledEvent notification,
            CancellationToken ct)
        {
            var existingConversation = await _context.Conversations
                .AnyAsync(c => c.AppointmentId == notification.AppointmentId, ct);

            if (!existingConversation)
            {
                var conversationResult = Conversation.CreateForAppointment(
                    notification.AppointmentId,
                    notification.UserId,
                    notification.SpecialistId
                );

                if (conversationResult.IsSuccess)
                {
                    _context.Conversations.Add(conversationResult.Value);
                    await _context.SaveChangesAsync(ct);
                }
            }

            await _notificationService.CreateAndSendNotificationAsync(
                notification.SpecialistId,
                "حجز استشارة جديد",
                $"لديك طلب حجز استشارة جديد من المستخدم.",
                NotificationType.Booking,
                ct);

            await _notificationService.CreateAndSendNotificationAsync(
                notification.UserId,
                "تم إرسال طلب الحجز",
                $"تم إرسال طلب حجز الاستشارة. يرجى انتظار تأكيد المختص.",
                NotificationType.Booking,
                ct);
        }
    }
}