using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using MediatR;
using Microsoft.AspNetCore.Identity;

public sealed class SpecialistOnboardedEventHandler : INotificationHandler<SpecialistOnboardedEvent>
{
    private readonly UserManager<User> _userManager;
    private readonly INotificationService _notificationService;

    public SpecialistOnboardedEventHandler(
        UserManager<User> userManager,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task Handle(
        SpecialistOnboardedEvent notification,
        CancellationToken cancellationToken)
    {
        var admins = await _userManager.GetUsersInRoleAsync(
            nameof(UserRole.Admin));

        var specialistUser = await _userManager.FindByIdAsync(
            notification.UserId.ToString());

        foreach (var admin in admins)
        {
            await _notificationService
                .CreateAndSendNotificationAsync(
                    admin.Id,
                    "New Specialist Verification Request",
                    $"{specialistUser?.Name} submitted a specialist profile for review.",
                    NotificationType.SpecialistVerification,
                    cancellationToken);
        }
    }
}