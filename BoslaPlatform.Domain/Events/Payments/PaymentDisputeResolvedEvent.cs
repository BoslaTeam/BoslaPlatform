using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payments;

public class PaymentDisputeResolvedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid AppointmentId { get; }
    public bool WasRefunded { get; }
    public string? AdminNotes { get; }

    public PaymentDisputeResolvedEvent(Guid paymentId, Guid appointmentId, bool wasRefunded, string? adminNotes)
    {
        PaymentId = paymentId;
        AppointmentId = appointmentId;
        WasRefunded = wasRefunded;
        AdminNotes = adminNotes;
    }
}
