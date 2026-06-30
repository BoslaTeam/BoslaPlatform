namespace BoslaPlatform.Application.Features.Admin.DTOs;

public sealed class AdminPaymentDetailDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserAvatarUrl { get; set; }
    public Guid SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string? SpecialistAvatarUrl { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ExternalPaymentId { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal SpecialistAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? RefundReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
