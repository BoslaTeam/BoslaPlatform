namespace BoslaPlatform.Application.Features.Wallets.DTOs;

public class WalletResponseDto
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public decimal HoldBalance { get; set; }
    public string Currency { get; set; } = "EGP";
    public List<TransactionDto> RecentTransactions { get; set; } = [];
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
