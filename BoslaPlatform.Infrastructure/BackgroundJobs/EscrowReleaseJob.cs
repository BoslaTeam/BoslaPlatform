using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Payouts;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

public sealed class EscrowReleaseJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscrowReleaseJob> _logger;

    public EscrowReleaseJob(IServiceScopeFactory scopeFactory, ILogger<EscrowReleaseJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EscrowReleaseJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredEscrowsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Escrow release cycle failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ReleaseExpiredEscrowsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var now = DateTime.UtcNow;

        var heldPayments = await context.Set<Payment>()
            .Include(p => p.Appointment)
            .Where(p => p.EscrowStatus == EscrowStatus.Held
                     && p.HeldUntil != null
                     && p.HeldUntil <= now)
            .ToListAsync(ct);

        foreach (var payment in heldPayments)
        {
            try
            {
                var specialistId = payment.Appointment.SpecialistId;

                // Credit specialist wallet
                var specialistWallet = await context.Set<SpecialistWallet>()
                    .FirstOrDefaultAsync(w => w.SpecialistId == specialistId, ct);

                if (specialistWallet is null)
                {
                    specialistWallet = new SpecialistWallet(specialistId);
                    context.Set<SpecialistWallet>().Add(specialistWallet);
                }

                specialistWallet.Credit(
                    payment.SpecialistAmount,
                    $"إفراج عن الدفعة المحجوزة - الجلسة {payment.AppointmentId}",
                    "Payment",
                    payment.Id);

                // Credit platform wallet (fees + tax)
                var platformWallet = await context.Set<PlatformWallet>()
                    .FirstOrDefaultAsync(ct);

                if (platformWallet is null)
                {
                    platformWallet = new PlatformWallet(Guid.Empty);
                    context.Set<PlatformWallet>().Add(platformWallet);
                }

                var platformAmount = payment.PlatformFeeAmount + payment.TaxAmount;
                if (platformAmount > 0)
                {
                    platformWallet.Credit(
                        platformAmount,
                        $"رسوم المنصة والضريبة - الجلسة {payment.AppointmentId}",
                        "Payment",
                        payment.Id);
                }

                payment.ReleaseFromEscrow();

                _logger.LogInformation(
                    "Released escrow for payment {PaymentId}, specialist {SpecialistId}, amount {Amount}",
                    payment.Id, specialistId, payment.SpecialistAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release escrow for payment {PaymentId}", payment.Id);
            }
        }

        if (heldPayments.Count > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Released escrow for {Count} payments", heldPayments.Count);
        }
    }
}
