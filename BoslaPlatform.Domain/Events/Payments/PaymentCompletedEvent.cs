using BoslaPlatform.Domain.Common;

namespace BoslaPlatform.Domain.Events.Payments
{
    public class PaymentCompletedEvent : DomainEvent
    {
        public Guid PaymentId { get; }
        public Guid AppointmentId { get; }
        public decimal Amount { get; }
        public string ExternalPaymentId { get; }

        public PaymentCompletedEvent(Guid paymentId, Guid appointmentId, decimal amount, string externalPaymentId)
        {
            PaymentId = paymentId;
            AppointmentId = appointmentId;
            Amount = amount;
            ExternalPaymentId = externalPaymentId;
        }
    }
}
