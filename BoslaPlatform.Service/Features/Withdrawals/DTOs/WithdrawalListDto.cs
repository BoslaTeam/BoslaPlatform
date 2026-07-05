namespace BoslaPlatform.Application.Features.Withdrawals.DTOs;

public class WithdrawalListDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string? SpecialistImage { get; set; }
    public string? SpecialistTitle { get; set; }
}
