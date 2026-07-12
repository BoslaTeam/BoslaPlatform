using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage.Cloudflare;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage;

public sealed class TemporaryFileCleanerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TemporaryFileCleanerOptions _options;
    private readonly ILogger<TemporaryFileCleanerService> _logger;

    public TemporaryFileCleanerService(
        IServiceScopeFactory scopeFactory,
        IOptions<TemporaryFileCleanerOptions> options,
        ILogger<TemporaryFileCleanerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TemporaryFileCleanerService started (retention: {RetentionMin}min, polling: {PollSec}s)",
            _options.RetentionMinutes, _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleaner = scope.ServiceProvider.GetRequiredService<ITemporaryFileCleaner>();

                await cleaner.CleanupAsync(
                    TimeSpan.FromMinutes(_options.RetentionMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Temporary file cleanup iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }
}