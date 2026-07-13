using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Threading.Tasks;

namespace BoslaPlatform.Infrastructure.Communication
{
    public sealed class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptionsSnapshot<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            email.ReplyTo.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            email.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
            email.Headers.Add("X-Mailer", "BoslaPlatform");
            email.Headers.Add("Precedence", "bulk");

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To} via Gmail SMTP: {Subject}", toEmail, subject);
        }
    }
}
