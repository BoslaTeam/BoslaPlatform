using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistVerificationRejectedHandler : INotificationHandler<SpecialistVerificationRejectedEvent>
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public SpecialistVerificationRejectedHandler(
            UserManager<User> userManager,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task Handle(
            SpecialistVerificationRejectedEvent notification,
            CancellationToken cancellationToken)
        {
            var specialistUser = await _userManager.FindByIdAsync(
                notification.SpecialistId.ToString());

            if (specialistUser is null)
                return;

            await _notificationService
                .CreateAndSendNotificationAsync(
                    specialistUser.Id,
                    "Verification Rejected",
                    "Your verification was rejected. Please review the admin notes and resubmit with the required corrections.",
                    NotificationType.SpecialistVerification,
                    cancellationToken);
        }
    }
}
