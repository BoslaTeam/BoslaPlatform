using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities.Payments;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Payments;
using BoslaPlatform.Domain.ValueObjects;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Payment : AuditableEntity
    {
        public Guid AppointmentId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = "usd";
        public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
        public string PaymentMethod { get; private set; } = string.Empty;
        public string? ExternalPaymentId { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public string? RefundReason { get; private set; }
        public decimal PlatformFeeAmount { get; private set; }
        public decimal SpecialistAmount { get; private set; }
        public decimal TaxAmount { get; private set; }

        // Escrow properties
        public EscrowStatus EscrowStatus { get; private set; } = EscrowStatus.Held;
        public DateTime? HeldUntil { get; private set; }
        public DateTime? ReleasedAt { get; private set; }
        public string? DisputeReason { get; private set; }
        public DateTime? DisputedAt { get; private set; }

        // Navigation
        public Appointment Appointment { get; private set; } = null!;
        public PaymentComplaint? Complaint { get; private set; }

        private Payment() { }

        // Factory Method
        public static Payment Initiate(
            Guid appointmentId,
            decimal hourlyRate,
            string currency = "usd")
        {
            decimal platformFeePercent = 0.10m;
            decimal taxPercent = 0.05m;

            decimal platformFee = Math.Round(hourlyRate * platformFeePercent, 2);
            decimal tax = Math.Round(hourlyRate * taxPercent, 2);
            decimal specialistEarnings = hourlyRate - platformFee;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Amount = hourlyRate + tax, 
                Currency = currency.ToLowerUtc(),
                PlatformFeeAmount = platformFee,
                TaxAmount = tax,
                SpecialistAmount = specialistEarnings,
                Status = PaymentStatus.Pending
            };

            return payment;
        }

        // Domain Behaviors
        public void AssignExternalId(string externalPaymentId)
        {
            ExternalPaymentId = externalPaymentId;
        }

        public void CompleteAndHold(string externalPaymentId, string paymentMethod)
        {
            if (Status == PaymentStatus.Completed) return;

            Status = PaymentStatus.Completed;
            ExternalPaymentId = externalPaymentId;
            PaymentMethod = paymentMethod;
            PaidAt = DateTime.UtcNow;
            EscrowStatus = EscrowStatus.Held;
            HeldUntil = DateTime.UtcNow.AddDays(14);

            AddDomainEvent(new PaymentCompletedEvent(Id, AppointmentId, Amount, ExternalPaymentId));
        }

        public void Complete(string externalPaymentId, string paymentMethod)
        {
            if (Status == PaymentStatus.Completed) return;

            Status = PaymentStatus.Completed;
            ExternalPaymentId = externalPaymentId;
            PaymentMethod = paymentMethod;
            PaidAt = DateTime.UtcNow;
            EscrowStatus = EscrowStatus.Released;
            ReleasedAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentCompletedEvent(Id, AppointmentId, Amount, ExternalPaymentId));
        }

        public void ReleaseFromEscrow()
        {
            if (EscrowStatus != EscrowStatus.Held)
                throw new InvalidOperationException("Only held payments can be released from escrow.");

            EscrowStatus = EscrowStatus.Released;
            ReleasedAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentEscrowReleasedEvent(
                Id, AppointmentId, SpecialistAmount, PlatformFeeAmount, TaxAmount));
        }

        public void FileDispute(string reason)
        {
            if (EscrowStatus != EscrowStatus.Held)
                throw new InvalidOperationException("Only held payments can be disputed.");

            EscrowStatus = EscrowStatus.Disputed;
            DisputeReason = reason;
            DisputedAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentDisputeFiledEvent(Id, AppointmentId, Appointment.UserId, reason));
        }

        public void RefundAfterDispute(string? reason)
        {
            if (EscrowStatus != EscrowStatus.Disputed)
                throw new InvalidOperationException("Only disputed payments can be refunded via dispute.");

            Status = PaymentStatus.Refunded;
            EscrowStatus = EscrowStatus.Refunded;
            RefundReason = reason;
        }

        public void RejectDispute()
        {
            if (EscrowStatus != EscrowStatus.Disputed)
                throw new InvalidOperationException("Only disputed payments can have dispute rejected.");

            EscrowStatus = EscrowStatus.Held;
            DisputeReason = null;
            DisputedAt = null;

            AddDomainEvent(new PaymentDisputeResolvedEvent(Id, AppointmentId, false, null));
        }

        public void MarkAsFailed(string? reason)
        {
            Status = PaymentStatus.Failed;
            RefundReason = reason;
        }

        public void MarkAsRefunded(string? reason)
        {
            if (Status != PaymentStatus.Completed)
                throw new InvalidOperationException("Only completed payments can be refunded.");

            Status = PaymentStatus.Refunded;
            EscrowStatus = EscrowStatus.Refunded;
            RefundReason = reason;
        }
    }
}

public static class StringExtensions
{
    public static string ToLowerUtc(this string input) => input?.ToLowerProviderInvariant() ?? string.Empty;
    private static string ToLowerProviderInvariant(this string input) => input.ToLowerInvariant();
}