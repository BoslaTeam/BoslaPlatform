using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistVerificationSubmittedHandler : INotificationHandler<SpecialistVerificationSubmittedEvent>
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public SpecialistVerificationSubmittedHandler(
            UserManager<User> userManager,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task Handle(
            SpecialistVerificationSubmittedEvent notification,
            CancellationToken cancellationToken)
        {
            var admins = await _userManager.GetUsersInRoleAsync(
                nameof(UserRole.Admin));

            var specialist = await _userManager.FindByIdAsync(
                notification.SpecialistId.ToString());

            foreach (var admin in admins)
            {
                await _notificationService
                    .CreateAndSendNotificationAsync(
                        admin.Id,
                        "New Verification Request",
                        $"{specialist?.Name ?? "A specialist"} submitted verification documents for review.",
                        NotificationType.SpecialistVerification,
                        cancellationToken);
            }
        }
    }
}
