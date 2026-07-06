using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Entities.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WalletDebitedEventHandler(
    IAppDbContext context,
    INotificationService notificationService)
    : INotificationHandler<WalletDebitedEvent>
{
    public async Task Handle(WalletDebitedEvent notification, CancellationToken ct)
    {
        var walletType = await GetWalletTypeAsync(notification.WalletId, ct);
        if (walletType == null) return;

        await notificationService.CreateAndSendNotificationAsync(
            notification.OwnerId,
            walletType == "specialist" ? "تم خصم من محفظتك" :
            walletType == "user" ? "تم خصم مبلغ من محفظتك" :
            "خصم من محفظة المنصة",
            $"تم خصم {notification.Amount:C} من محفظتك. الرصيد الحالي: {notification.NewBalance:C}. {notification.Description}",
            NotificationType.WalletDebit,
            ct);
    }

    private async Task<string?> GetWalletTypeAsync(Guid walletId, CancellationToken ct)
    {
        if (await context.Set<SpecialistWallet>().AnyAsync(w => w.Id == walletId, ct))
            return "specialist";
        if (await context.Set<UserWallet>().AnyAsync(w => w.Id == walletId, ct))
            return "user";
        if (await context.Set<PlatformWallet>().AnyAsync(w => w.Id == walletId, ct))
            return "platform";
        return null;
    }
}
