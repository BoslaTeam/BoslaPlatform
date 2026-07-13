using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace BoslaPlatform.Infrastructure.Communication
{
    public sealed class SendGridEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IOptionsSnapshot<EmailSettings> settings, ILogger<SendGridEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);

            msg.SetClickTracking(false, false);
            msg.SetOpenTracking(false);
            msg.SetGoogleAnalytics(false);

            var response = await client.SendEmailAsync(msg);
            var statusCode = (int)response.StatusCode;

            if (statusCode >= 200 && statusCode < 300)
            {
                _logger.LogInformation("Email sent to {To} via SendGrid (status {Status})", toEmail, statusCode);
            }
            else
            {
                var bodyText = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid send failed to {To}: status {Status}, body: {Body}",
                    toEmail, statusCode, bodyText);
            }
        }
    }
}
