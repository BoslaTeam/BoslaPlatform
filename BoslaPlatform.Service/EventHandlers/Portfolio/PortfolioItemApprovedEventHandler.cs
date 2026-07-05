using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BoslaPlatform.Application.Features.Notifications.Services;

namespace BoslaPlatform.Service.EventHandlers.Portfolio
{
    public sealed class PortfolioItemApprovedEventHandler : INotificationHandler<PortfolioItemApprovedEvent>
    {
        private readonly IAppDbContext _context;
        private readonly INotificationService _notificationService;

        public PortfolioItemApprovedEventHandler(IAppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task Handle(PortfolioItemApprovedEvent notification, CancellationToken ct)
        {
            var specialist = await _context.Set<Specialist>()
                .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

            if (specialist == null) return;

            await _notificationService.CreateAndSendNotificationAsync(
                specialist.UserId,
                "تم الموافقة على العمل",
                $"تمت الموافقة على عملك \"{notification.Title}\" في معرض الأعمال وهو الآن مرئي للعملاء.",
                NotificationType.PortfolioApproved,
                ct);
        }
    }
}
