namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class SpecialistEarningsDto
    {
        public decimal TotalEarnings { get; set; }
        public decimal WithdrawableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public List<EarningHistoryItemDto> History { get; set; } = new();
    }
}
