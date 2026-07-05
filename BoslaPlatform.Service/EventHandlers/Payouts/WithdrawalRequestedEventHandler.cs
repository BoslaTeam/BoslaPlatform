using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Payouts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WithdrawalRequestedEventHandler(
    IAppDbContext context,
    INotificationService notificationService,
    ILogger<WithdrawalRequestedEventHandler> logger)
    : INotificationHandler<WithdrawalRequestedEvent>
{
    public async Task Handle(WithdrawalRequestedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Withdrawal {Id} requested by specialist {SpecialistId}, amount {Amount}",
            notification.WithdrawalId, notification.SpecialistId, notification.Amount);
    }
}
