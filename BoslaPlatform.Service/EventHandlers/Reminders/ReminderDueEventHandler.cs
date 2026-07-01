using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Reminders;
using MediatR;

namespace BoslaPlatform.Application.EventHandlers.Reminders
{
    public sealed class ReminderDueEventHandler
        : INotificationHandler<ReminderDueEvent>
    {
        private readonly INotificationService _notificationService;

        public ReminderDueEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(
            ReminderDueEvent notification,
            CancellationToken ct)
        {
            await _notificationService.CreateAndSendNotificationAsync(
                notification.UserId,
                "تذكير بموعد الاستشارة",
                notification.Message,
                NotificationType.Reminder,
                ct);
        }
    }
}