using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistVerificationSubmittedHandler : INotificationHandler<SpecialistVerificationSubmittedEvent>
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAppDbContext _context;

        public SpecialistVerificationSubmittedHandler(
            UserManager<User> userManager,
            INotificationService notificationService,
            IAppDbContext context)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task Handle(
            SpecialistVerificationSubmittedEvent notification,
            CancellationToken cancellationToken)
        {
            var admins = await _userManager.GetUsersInRoleAsync(
                nameof(UserRole.Admin));

            var specialist = await _context.Specialists
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, cancellationToken);

            var specialistName = specialist?.User?.Name ?? "أحد المتخصصين";

            foreach (var admin in admins)
            {
                await _notificationService
                    .CreateAndSendNotificationAsync(
                        admin.Id,
                        "طلب توثيق جديد",
                        $"{specialistName} قام بتقديم مستندات التوثيق للمراجعة.",
                        NotificationType.SpecialistVerification,
                        cancellationToken);
            }
        }
    }
}
