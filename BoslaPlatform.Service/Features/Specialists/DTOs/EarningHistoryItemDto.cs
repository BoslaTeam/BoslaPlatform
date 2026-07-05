namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class EarningHistoryItemDto
    {
        public Guid PaymentId { get; set; }
        public Guid AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public DateTime PaidAt { get; set; }
        public string ClientName { get; set; } = string.Empty;
    }
}
