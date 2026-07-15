namespace BoslaPlatform.Application.Features.Withdrawals.DTOs;

public class WalletDto
{
    public decimal TotalEarnings { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal PendingReleaseBalance { get; set; }
    public DateTime? NextReleaseDate { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public List<WithdrawalDto> RecentWithdrawals { get; set; } = [];
}
