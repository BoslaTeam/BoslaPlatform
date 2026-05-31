using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.ValueObjects;

namespace BoslaPlatform.Domain.Models.Booking
{
    public class Payment: AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public decimal Amount { get; }

        public string Currency { get; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ExternalPaymentId { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? RefundReason { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal SpecialistAmount { get; set; }
        public decimal TaxAmount { get; set; }

        // Navigation
        public Appointment Appointment { get; set; } = null!;
    }
}
