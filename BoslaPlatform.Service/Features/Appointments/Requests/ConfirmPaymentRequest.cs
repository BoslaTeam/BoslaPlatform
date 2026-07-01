namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class ConfirmPaymentRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;

        public ConfirmPaymentRequest() { }

        public ConfirmPaymentRequest(string paymentIntentId)
        {
            PaymentIntentId = paymentIntentId;
        }
    }
}
