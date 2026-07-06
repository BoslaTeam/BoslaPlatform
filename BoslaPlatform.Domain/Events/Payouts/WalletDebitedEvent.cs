using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Entities.Payouts;

public sealed class WalletDebitedEvent : DomainEvent
{
    public Guid WalletId { get; }
    public Guid OwnerId { get; }
    public decimal Amount { get; }
    public decimal NewBalance { get; }
    public string Description { get; }

    public WalletDebitedEvent(Guid walletId, Guid ownerId, decimal amount, decimal newBalance, string description)
    {
        WalletId = walletId; OwnerId = ownerId; Amount = amount; NewBalance = newBalance; Description = description;
    }
}
