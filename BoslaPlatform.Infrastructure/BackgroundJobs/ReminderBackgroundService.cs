using BoslaPlatform.Application.Features.Notifications.Services;
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

        var now = DateTime.UtcNow;

        var due = await context.Set<Reminder>()
            .Where(r => !r.IsSent && r.ReminderTime <= now)
            .ToListAsync(ct);

        foreach (var reminder in due)
        {
            try
            {
                await notificationService.CreateAndSendNotificationAsync(
                    reminder.UserId,
                    "تذكير بالجلسة",
                    reminder.Message,
                    NotificationType.Reminder,
                    ct,
                    appointmentId: reminder.AppointmentId);

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
        }
    }
}
