using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistVerificationApprovedHandler : INotificationHandler<SpecialistVerificationApprovedEvent>
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public SpecialistVerificationApprovedHandler(
            UserManager<User> userManager,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task Handle(
            SpecialistVerificationApprovedEvent notification,
            CancellationToken cancellationToken)
        {
            var specialistUser = await _userManager.FindByIdAsync(
                notification.SpecialistId.ToString());

            if (specialistUser is null)
                return;

            await _notificationService
                .CreateAndSendNotificationAsync(
                    specialistUser.Id,
                    "Verification Approved",
                    "Congratulations! Your verification has been approved.",
                    NotificationType.SpecialistVerification,
                    cancellationToken);
        }
    }
}
