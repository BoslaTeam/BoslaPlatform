namespace BoslaPlatform.Application.Features.Payments.Requests
{
    public class InitiatePaymentRequest
    {
        public Guid AppointmentId { get; set; }
        public string Currency { get; set; } = "usd";
    }
}
