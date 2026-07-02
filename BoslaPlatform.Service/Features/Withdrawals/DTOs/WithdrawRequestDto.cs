namespace BoslaPlatform.Application.Features.Withdrawals.DTOs;

public class WithdrawRequestDto
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentDetails { get; set; }
}
