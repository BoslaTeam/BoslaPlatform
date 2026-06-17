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
<div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; background-color: #f9f9fc; padding: 40px 20px; text-align: center;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea;'>
        <div style='margin-bottom: 30px;'>
            <h1 style='color: #2c3e50; font-size: 24px; margin: 0;'>Bosla Platform</h1>
        </div>
        <div style='margin-bottom: 20px;'>
            <span style='font-size: 48px;'>🎉</span>
        </div>
        <h2 style='color: #333333; font-size: 22px; margin-bottom: 15px;'>Welcome to Bosla, {notification.UserName}!</h2>
        <p style='color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>
            Your email has been verified successfully. You're all set to explore everything Bosla has to offer!
        </p>
        <p style='color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
            Start by logging into your account and discover our amazing specialists, book consultations, and take your journey to the next level.
        </p>
        <a href='https://localhost:44397/login' style='display: inline-block; background-color: #4361ee; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 16px; font-weight: 600; border-radius: 6px;'>
            Get Started
        </a>
        <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;'>
        <p style='color: #aaaaaa; font-size: 12px; margin: 0;'>
            &copy; {DateTime.UtcNow.Year} Bosla Platform. All rights reserved.
        </p>
    </div>
</div>";

            await _emailService.SendEmailAsync(
                notification.Email,
                "Welcome to Bosla Platform! 🎉",
                body);

            _logger.LogInformation(
                "Welcome email sent successfully to {Email}", notification.Email);
        }
    }
}
