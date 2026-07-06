using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Entities.Payouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Payouts;

public sealed class WalletCreditedEventHandler(
    IAppDbContext context,
    INotificationService notificationService)
    : INotificationHandler<WalletCreditedEvent>
{
    public async Task Handle(WalletCreditedEvent notification, CancellationToken ct)
    {
        var walletType = await GetWalletTypeAsync(notification.WalletId, ct);
        if (walletType == null) return;

        await notificationService.CreateAndSendNotificationAsync(
            notification.OwnerId,
            walletType == "specialist" ? "تم إيداع أرباح جديدة" :
            walletType == "user" ? "تم إيداع مبلغ في محفظتك" :
            "إيداع في محفظة المنصة",
            $"تم إضافة {notification.Amount:C} إلى محفظتك. الرصيد الحالي: {notification.NewBalance:C}. {notification.Description}",
            NotificationType.WalletCredit,
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
