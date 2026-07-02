using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payouts;

public sealed class WithdrawalRejectedEvent : DomainEvent
{
    public Guid WithdrawalId { get; }
    public Guid SpecialistId { get; }
    public string? AdminNotes { get; }

    public WithdrawalRejectedEvent(Guid withdrawalId, Guid specialistId, string? adminNotes)
    {
        WithdrawalId = withdrawalId;
        SpecialistId = specialistId;
        AdminNotes = adminNotes;
    }
}
