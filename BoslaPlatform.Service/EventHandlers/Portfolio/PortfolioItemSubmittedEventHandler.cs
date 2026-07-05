using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Service.EventHandlers.Portfolio
{
    public sealed class PortfolioItemSubmittedEventHandler : INotificationHandler<PortfolioItemSubmittedEvent>
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAppDbContext _context;
        private readonly ILogger<PortfolioItemSubmittedEventHandler> _logger;

        public PortfolioItemSubmittedEventHandler(
            UserManager<User> userManager,
            INotificationService notificationService,
            IAppDbContext context,
            ILogger<PortfolioItemSubmittedEventHandler> logger)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task Handle(PortfolioItemSubmittedEvent notification, CancellationToken ct)
        {
            _logger.LogInformation(
                "Portfolio item '{Title}' ({ItemId}) submitted by specialist {SpecialistId}",
                notification.Title, notification.PortfolioItemId, notification.SpecialistId);

            var specialist = await _context.Set<Domain.Entities.Profile.Specialist>()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, ct);

            var specialistName = specialist?.User?.Name ?? "أحد المتخصصين";
            var admins = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Admin));

            foreach (var admin in admins)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                    admin.Id,
                    "طلب مراجعة معرض أعمال",
                    $"{specialistName} قام بإضافة عمل \"{notification.Title}\" في معرض الأعمال ويحتاج للمراجعة.",
                    NotificationType.PortfolioPendingReview,
                    ct);
            }
        }
    }
}
