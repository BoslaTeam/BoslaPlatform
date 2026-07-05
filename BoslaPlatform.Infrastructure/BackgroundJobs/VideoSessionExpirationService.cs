using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

public sealed class VideoSessionExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoSessionExpirationService> _logger;
    private readonly VideoSessionExpirationOptions _options;

    public VideoSessionExpirationService(
        IServiceScopeFactory scopeFactory,
        IOptions<VideoSessionExpirationOptions> options,
        ILogger<VideoSessionExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "VideoSessionExpirationService started (batchSize: {BatchSize}, pollingInterval: {Interval}s)",
            _options.BatchSize, _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session expiration cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessExpiredSessionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var lifecycleService = scope.ServiceProvider.GetRequiredService<IVideoSessionLifecycleService>();

        bool hasMore;
        do
        {
            ct.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;

            var batch = await context.VideoSessions
                .Include(vs => vs.Appointment)
                .Where(vs => vs.Status != VideoSessionStatus.Completed)
                .Take(_options.BatchSize)
                .ToListAsync(ct);

            var expired = batch
                .Where(vs => vs.Appointment is not null
                    && vs.Appointment.VideoSessionExpirationTime <= now)
                .ToList();

            hasMore = expired.Count == _options.BatchSize;

            foreach (var session in expired)
            {
                try
                {
                    var result = await lifecycleService.CompleteSessionAsync(
                        session.Id, VideoSessionCompletionReason.AppointmentExpired, ct);

                    if (result.IsError)
                    {
                        _logger.LogWarning(
                            "Failed to expire session {SessionId}", session.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error expiring session {SessionId}",
                        session.Id);
                }
            }
        }
        while (hasMore);
    }
}
