using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Reminders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.BackgroundJobs
{
    public sealed class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var now = DateTimeOffset.UtcNow;

                    var dueReminders = await context.Set<Domain.Models.Booking.Reminder>()
                        .Where(r => !r.IsSent && r.ReminderTime <= now)
                        .ToListAsync(stoppingToken);

                    foreach (var reminder in dueReminders)
                    {
                        reminder.IsSent = true;

                        await mediator.Publish(
                            new ReminderDueEvent(
                                reminder.Id,
                                reminder.AppointmentId,
                                reminder.UserId,
                                reminder.Message),
                            stoppingToken);
                    }

                    if (dueReminders.Count > 0)
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Processed {Count} due reminders.", dueReminders.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminders.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("ReminderBackgroundService stopped.");
        }
    }
}