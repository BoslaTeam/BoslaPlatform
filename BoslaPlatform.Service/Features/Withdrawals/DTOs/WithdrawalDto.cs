namespace BoslaPlatform.Application.Features.Withdrawals.DTOs;

public class WithdrawalDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentDetails { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? AdminNotes { get; set; }
}
