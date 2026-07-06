namespace BoslaPlatform.Application.Features.Wallets.DTOs;

public class AdminWalletStatsDto
{
    public decimal TotalPlatformFees { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalPaidToSpecialists { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal AvailableBalance { get; set; }
    public int TotalCompletedPayments { get; set; }
    public int TotalRefundedPayments { get; set; }
}
