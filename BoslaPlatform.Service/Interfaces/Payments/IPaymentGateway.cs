namespace BoslaPlatform.Application.Interfaces.Payments
{
    public interface IPaymentGateway
    {
        Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(decimal amount, string currency);
        Task<string?> GetPaymentIntentClientSecretAsync(string paymentIntentId);
    }
}
