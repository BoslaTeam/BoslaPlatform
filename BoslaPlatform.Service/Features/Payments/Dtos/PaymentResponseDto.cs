using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Payments.Dtos
{
    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public PaymentStatus Status { get; set; }
        public EscrowStatus EscrowStatus { get; set; }
        public DateTime? HeldUntil { get; set; }
        public string? DisputeReason { get; set; }
        public DateTime? DisputedAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ExternalPaymentId { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal SpecialistAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? ClientSecret { get; set; }
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
