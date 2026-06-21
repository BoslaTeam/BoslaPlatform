using BoslaPlatform.Application.Interfaces.Payments;
using BoslaPlatform.Application.Settings;
using Microsoft.Extensions.Options;
using Stripe;

namespace BoslaPlatform.Application.Features.Payments.Services
{
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly StripeSettings _settings;

        public StripePaymentGateway(IOptions<StripeSettings> settings)
        {
            _settings = settings.Value;
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public async Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(decimal amount, string currency)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLowerProviderInvariant(),
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(options);

            return (intent.ClientSecret, intent.Id);
        }
    }

    internal static class StringExtensions
    {
        public static string ToLowerProviderInvariant(this string input) => input?.ToLowerInvariant() ?? string.Empty;
    }
}
