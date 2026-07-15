using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Entities.Payments;

public class PaymentComplaint : AuditableEntity
{
    public Guid PaymentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ComplaintStatus Status { get; private set; } = ComplaintStatus.Pending;
    public Guid? ReviewedBy { get; private set; }
    public string? AdminNotes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public Models.Booking.Payment Payment { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private PaymentComplaint() { }

    public static PaymentComplaint File(Guid paymentId, Guid userId, string reason, string? description)
    {
        return new PaymentComplaint
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            UserId = userId,
            Reason = reason,
            Description = description,
            Status = ComplaintStatus.Pending
        };
    }

    public void Review(Guid adminId)
    {
        ReviewedBy = adminId;
        Status = ComplaintStatus.Reviewed;
    }

    public void ResolveRefunded(Guid adminId, string? notes)
    {
        ReviewedBy = adminId;
        AdminNotes = notes;
        Status = ComplaintStatus.ResolvedRefunded;
        ResolvedAt = DateTime.UtcNow;
    }

    public void ResolveRejected(Guid adminId, string? notes)
    {
        ReviewedBy = adminId;
        AdminNotes = notes;
        Status = ComplaintStatus.ResolvedRejected;
        ResolvedAt = DateTime.UtcNow;
    }
}
