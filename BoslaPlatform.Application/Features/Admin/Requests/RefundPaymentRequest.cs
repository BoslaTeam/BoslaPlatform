namespace BoslaPlatform.Application.Features.Admin.Requests
{
    public sealed class RefundPaymentRequest
    {
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
    }
}
