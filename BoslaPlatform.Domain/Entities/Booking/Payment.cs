using BoslaPlatform.Domain.Common;
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

        // Navigation
        public Appointment Appointment { get; private set; } = null!;

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
        public void Complete(string externalPaymentId, string paymentMethod)
        {
            if (Status == PaymentStatus.Completed) return;

            Status = PaymentStatus.Completed;
            ExternalPaymentId = externalPaymentId;
            PaymentMethod = paymentMethod;
            PaidAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentCompletedEvent(Id, AppointmentId, Amount, ExternalPaymentId));
        }

        public void MarkAsFailed(string? reason)
        {
            Status = PaymentStatus.Failed;
            RefundReason = reason;
        }
    }
}

public static class StringExtensions
{
    public static string ToLowerUtc(this string input) => input?.ToLowerProviderInvariant() ?? string.Empty;
    private static string ToLowerProviderInvariant(this string input) => input.ToLowerInvariant();
}