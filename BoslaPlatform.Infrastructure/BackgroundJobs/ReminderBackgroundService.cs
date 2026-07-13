using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

public sealed class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reminder processing cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;

        var due = await context.Set<Reminder>()
            .Include(r => r.User)
            .Where(r => !r.IsSent && r.ReminderTime <= now)
            .ToListAsync(ct);

        foreach (var reminder in due)
        {
            try
            {
                var (title, subjectText) = GetReminderInfo(reminder.Message);

                await notificationService.CreateAndSendNotificationAsync(
                    reminder.UserId,
                    title,
                    reminder.Message,
                    NotificationType.Reminder,
                    ct,
                    appointmentId: reminder.AppointmentId);

                // Send email to user with the reminder
                var appointment = await context.Set<Appointment>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == reminder.AppointmentId, ct);

                var emailBody = BuildEmailBody(reminder.User.Name, reminder.Message, appointment);

                try
                {
                    await emailService.SendEmailAsync(reminder.User.Email, subjectText, emailBody);
                    _logger.LogInformation("Reminder email sent to {Email}: {Subject}", reminder.User.Email, subjectText);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder email to {Email}", reminder.User.Email);
                }

                reminder.IsSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send reminder {ReminderId}", reminder.Id);
            }
        }

        if (due.Count > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Processed {Count} due reminders", due.Count);
        }
    }

    private static (string title, string subject) GetReminderInfo(string message)
    {
        if (message.Contains("مهلة") || message.Contains("الدفع"))
            return ("تذكير بالدفع", "تذكير: مهلة الدفع على وشك الانتهاء");
        if (message.Contains("الانضمام") || message.Contains("انضم"))
            return ("تذكير بالجلسة", "يمكنك الآن الانضمام إلى الجلسة");
        return ("تذكير بالجلسة", "موعد جلستك سيبدأ قريباً");
    }

    private static string BuildEmailBody(string userName, string message, Appointment? appointment)
    {
        var body = $@"
        <div dir='rtl' style='font-family: Arial, sans-serif;'>
            <h2>{message}</h2>
            <p>مرحباً {userName}،</p>
            <p>{message}</p>";

        if (appointment is not null)
        {
            body += $@"
            <br>
            <b>تاريخ الموعد:</b> {appointment.Start:yyyy-MM-dd}<br>
            <b>الوقت:</b> {appointment.Start:hh:mm tt} - {appointment.End:hh:mm tt}";
        }

        body += $@"
            <br>
            <hr>
            <p style='color: #999; font-size: 11px;'>Bosla Platform &copy; {DateTime.UtcNow.Year}</p>
        </div>";

        return body;
    }
}
