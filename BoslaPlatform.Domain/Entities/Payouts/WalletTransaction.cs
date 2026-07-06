using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Entities.Payouts;

public class WalletTransaction : BaseEntity
{
    public Guid WalletId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Wallet Wallet { get; private set; } = null!;

    private WalletTransaction() { }

    public WalletTransaction(Guid walletId, decimal amount, TransactionType type, string description, string? referenceType = null, Guid? referenceId = null)
    {
        Id = Guid.NewGuid();
        WalletId = walletId;
        Amount = amount;
        Type = type;
        Description = description;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
}
