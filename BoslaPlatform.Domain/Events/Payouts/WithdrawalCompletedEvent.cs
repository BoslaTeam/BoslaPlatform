using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payouts;

public sealed class WithdrawalCompletedEvent : DomainEvent
{
    public Guid WithdrawalId { get; }
    public Guid SpecialistId { get; }
    public decimal Amount { get; }

    public WithdrawalCompletedEvent(Guid withdrawalId, Guid specialistId, decimal amount)
    {
        WithdrawalId = withdrawalId;
        SpecialistId = specialistId;
        Amount = amount;
    }
}
