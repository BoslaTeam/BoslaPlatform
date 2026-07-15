using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payments;

public class PaymentEscrowReleasedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid AppointmentId { get; }
    public decimal SpecialistAmount { get; }
    public decimal PlatformFeeAmount { get; }
    public decimal TaxAmount { get; }

    public PaymentEscrowReleasedEvent(
        Guid paymentId,
        Guid appointmentId,
        decimal specialistAmount,
        decimal platformFeeAmount,
        decimal taxAmount)
    {
        PaymentId = paymentId;
        AppointmentId = appointmentId;
        SpecialistAmount = specialistAmount;
        PlatformFeeAmount = platformFeeAmount;
        TaxAmount = taxAmount;
    }
}
