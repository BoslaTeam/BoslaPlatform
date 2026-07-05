namespace BoslaPlatform.Application.Features.Admin.DTOs;

public sealed class AdminPaymentDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string SpecialistName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
