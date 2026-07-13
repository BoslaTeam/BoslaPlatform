using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

public sealed class AutoCancelUnpaidAppointmentsService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoCancelUnpaidAppointmentsService> _logger;

    public AutoCancelUnpaidAppointmentsService(IServiceScopeFactory scopeFactory, ILogger<AutoCancelUnpaidAppointmentsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoCancelUnpaidAppointmentsService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelExpiredAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-cancel cycle failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CancelExpiredAppointmentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var deadline = DateTimeOffset.UtcNow.AddHours(-6);

        var expired = await context.Set<Appointment>()
            .Include(a => a.StatusHistory)
            .Include(a => a.Payment)
            .Where(a => a.Status == AppointmentStatus.Confirmed
                     && a.ConfirmedAt != null
                     && a.ConfirmedAt <= deadline)
            .ToListAsync(ct);

        foreach (var appointment in expired)
        {
            try
            {
                appointment.Cancel(Guid.Empty, "Auto-cancelled: payment deadline of 6 hours exceeded.");
                _logger.LogInformation("Auto-cancelled appointment {AppointmentId}", appointment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-cancel appointment {AppointmentId}", appointment.Id);
            }
        }

        if (expired.Count > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Auto-cancelled {Count} expired appointments", expired.Count);
        }
    }
}
