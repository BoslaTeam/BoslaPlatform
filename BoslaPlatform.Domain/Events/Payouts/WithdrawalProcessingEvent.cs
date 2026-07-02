using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payouts;

public sealed class WithdrawalProcessingEvent : DomainEvent
{
    public Guid WithdrawalId { get; }
    public Guid SpecialistId { get; }

    public WithdrawalProcessingEvent(Guid withdrawalId, Guid specialistId)
    {
        WithdrawalId = withdrawalId;
        SpecialistId = specialistId;
    }
}
