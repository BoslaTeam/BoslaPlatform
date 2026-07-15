using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payments;

public class PaymentDisputeFiledEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid AppointmentId { get; }
    public Guid UserId { get; }
    public string Reason { get; }

    public PaymentDisputeFiledEvent(Guid paymentId, Guid appointmentId, Guid userId, string reason)
    {
        PaymentId = paymentId;
        AppointmentId = appointmentId;
        UserId = userId;
        Reason = reason;
    }
}
