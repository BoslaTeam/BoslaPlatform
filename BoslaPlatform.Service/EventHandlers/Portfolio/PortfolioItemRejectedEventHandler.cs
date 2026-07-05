using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BoslaPlatform.Application.Features.Notifications.Services;

namespace BoslaPlatform.Service.EventHandlers.Portfolio
{
    public sealed class PortfolioItemRejectedEventHandler : INotificationHandler<PortfolioItemRejectedEvent>
    {
        private readonly IAppDbContext _context;
        private readonly INotificationService _notificationService;

        public PortfolioItemRejectedEventHandler(IAppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task Handle(PortfolioItemRejectedEvent notification, CancellationToken ct)
        {
            var specialist = await _context.Set<Specialist>()
                .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

            if (specialist == null) return;

            var msg = string.IsNullOrEmpty(notification.Reason)
                ? $"تم رفض عملك \"{notification.Title}\" في معرض الأعمال."
                : $"تم رفض عملك \"{notification.Title}\" في معرض الأعمال. السبب: {notification.Reason}";

            await _notificationService.CreateAndSendNotificationAsync(
                specialist.UserId,
                "تم رفض العمل",
                msg,
                NotificationType.PortfolioRejected,
                ct);
        }
    }
}
