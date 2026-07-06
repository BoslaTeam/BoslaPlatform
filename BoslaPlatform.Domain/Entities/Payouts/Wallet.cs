using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Events.Payouts;

namespace BoslaPlatform.Domain.Entities.Payouts;

public abstract class Wallet : AuditableEntity
{
    public Guid OwnerId { get; protected set; }
    public decimal Balance { get; protected set; }
    public decimal HoldBalance { get; protected set; }
    public string Currency { get; protected set; } = "EGP";

    private readonly List<WalletTransaction> _transactions = [];
    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    protected Wallet() { }

    protected Wallet(Guid ownerId, string currency = "EGP")
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Balance = 0;
        HoldBalance = 0;
        Currency = currency;
    }

    public void Credit(decimal amount, string description, string? referenceType = null, Guid? referenceId = null)
    {
        if (amount <= 0) return;
        Balance += amount;
        _transactions.Add(new WalletTransaction(Id, amount, TransactionType.Credit, description, referenceType, referenceId));
        AddDomainEvent(new WalletCreditedEvent(Id, OwnerId, amount, Balance, description));
    }

    public bool TryDebit(decimal amount, string description, string? referenceType = null, Guid? referenceId = null)
    {
        if (amount <= 0 || amount > Balance) return false;
        Balance -= amount;
        _transactions.Add(new WalletTransaction(Id, amount, TransactionType.Debit, description, referenceType, referenceId));
        AddDomainEvent(new WalletDebitedEvent(Id, OwnerId, amount, Balance, description));
        return true;
    }

    public bool TryHold(decimal amount, string description, string? referenceType = null, Guid? referenceId = null)
    {
        if (amount <= 0 || amount > Balance) return false;
        Balance -= amount;
        HoldBalance += amount;
        _transactions.Add(new WalletTransaction(Id, amount, TransactionType.Hold, description, referenceType, referenceId));
        return true;
    }

    public void ReleaseHold(decimal amount, string description, string? referenceType = null, Guid? referenceId = null)
    {
        if (amount <= 0 || amount > HoldBalance) return;
        HoldBalance -= amount;
        _transactions.Add(new WalletTransaction(Id, amount, TransactionType.Release, description, referenceType, referenceId));
    }

    public void ReleaseHoldAndDebit(decimal amount, string description, string? referenceType = null, Guid? referenceId = null)
    {
        if (amount <= 0 || amount > HoldBalance) return;
        HoldBalance -= amount;
        _transactions.Add(new WalletTransaction(Id, amount, TransactionType.Debit, description, referenceType, referenceId));
        AddDomainEvent(new WalletDebitedEvent(Id, OwnerId, amount, Balance, description));
    }
}
