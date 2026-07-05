using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payouts;

namespace BoslaPlatform.Domain.Entities.Payouts;

public class Withdrawal : AuditableEntity
{
    public Guid SpecialistId { get; set; }
    public decimal Amount { get; set; }
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentDetails { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? AdminNotes { get; set; }

    public Specialist Specialist { get; set; } = null!;

    public static Withdrawal Request(Guid specialistId, decimal amount, string paymentMethod, string? paymentDetails)
    {
        var w = new Withdrawal
        {
            Id = Guid.NewGuid(),
            SpecialistId = specialistId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentDetails = paymentDetails,
            Status = WithdrawalStatus.Pending
        };
        w.AddDomainEvent(new WithdrawalRequestedEvent(w.Id, specialistId, amount));
        return w;
    }

    public void Approve(Guid adminId)
    {
        Status = WithdrawalStatus.Processing;
        ReviewedBy = adminId;
        AddDomainEvent(new WithdrawalProcessingEvent(Id, SpecialistId));
    }

    public void Complete()
    {
        Status = WithdrawalStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new WithdrawalCompletedEvent(Id, SpecialistId, Amount));
    }

    public void Reject(Guid adminId, string? notes)
    {
        Status = WithdrawalStatus.Rejected;
        ReviewedBy = adminId;
        AdminNotes = notes;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new WithdrawalRejectedEvent(Id, SpecialistId, notes));
    }
}
