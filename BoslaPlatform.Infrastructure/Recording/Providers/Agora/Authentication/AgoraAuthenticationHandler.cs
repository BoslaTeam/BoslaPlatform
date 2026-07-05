using System.Net.Http.Headers;
using System.Text;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Authentication
{
    internal sealed class AgoraAuthenticationHandler : DelegatingHandler
    {
        private readonly AgoraSettings _settings;

        public AgoraAuthenticationHandler(IOptions<AgoraSettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.CustomerId))
                throw new InvalidOperationException(
                    "Agora Cloud Recording CustomerId is not configured. " +
                    "Set 'AgoraSettings:CustomerId' in your configuration.");

            if (string.IsNullOrWhiteSpace(_settings.CustomerSecret))
                throw new InvalidOperationException(
                    "Agora Cloud Recording CustomerSecret is not configured. " +
                    "Set 'AgoraSettings:CustomerSecret' in your configuration.");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.CustomerId}:{_settings.CustomerSecret}"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
