using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Domain.Events.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.Authentication.EventHandlers
{
    public sealed class EmailVerifiedEventHandler : INotificationHandler<EmailVerifiedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerifiedEventHandler> _logger;

        public EmailVerifiedEventHandler(
            IEmailService emailService,
            ILogger<EmailVerifiedEventHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(EmailVerifiedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Sending welcome email to {Email} for user {UserId}",
                notification.Email, notification.UserId);

            var body = $@"
<div style='font-family: ""Inter"", ""Cairo"", Tahoma, Geneva, Verdana, sans-serif; background-color: #F7F9FA; padding: 40px 20px; text-align: center; direction: ltr;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea;'>
        <div style='margin-bottom: 30px;'>
            <h1 style='color: #1B4F72; font-size: 28px; font-weight: 700; margin: 0;'>Bosla</h1>
        </div>
        <h2 style='color: #2C3E50; font-size: 22px; margin-bottom: 15px;'>Welcome to Bosla, {notification.UserName}!</h2>
        <p style='color: #2C3E50; font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>
            Your email has been verified successfully. You're all set to explore everything Bosla has to offer!
        </p>
        <p style='color: #2C3E50; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
            Find the right expert. Book in minutes. Start by logging into your account and discover our amazing specialists.
        </p>
        <a href='http://localhost:4200/auth/login' style='display: inline-block; background-color: #F39C12; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 16px; font-weight: 600; border-radius: 6px;'>
            Get Started
        </a>
        <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;'>
        <p style='color: #95A5A6; font-size: 12px; margin: 0;'>
            &copy; {DateTime.UtcNow.Year} Bosla Platform. All rights reserved.<br>
            Your Compass to the Right Expert.
        </p>
    </div>
</div>";

            await _emailService.SendEmailAsync(
                notification.Email,
                "Welcome to Bosla Platform!",
                body);

            _logger.LogInformation(
                "Welcome email sent successfully to {Email}", notification.Email);
        }
    }
}
