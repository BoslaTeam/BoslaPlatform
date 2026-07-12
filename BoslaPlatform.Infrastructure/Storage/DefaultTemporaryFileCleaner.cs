using BoslaPlatform.Application.Interfaces.Storage;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Storage;

public sealed class DefaultTemporaryFileCleaner : ITemporaryFileCleaner
{
    private readonly ILogger<DefaultTemporaryFileCleaner> _logger;

    public DefaultTemporaryFileCleaner(ILogger<DefaultTemporaryFileCleaner> logger)
    {
        _logger = logger;
    }

    public Task CleanupAsync(TimeSpan? retention = null, CancellationToken ct = default)
    {
        var retentionTime = retention ?? TimeSpan.FromHours(1);
        var tempDir = Path.GetTempPath();
        var cutoff = DateTime.UtcNow.Subtract(retentionTime);
        var deletedCount = 0;

        try
        {
            var tempFiles = Directory.GetFiles(tempDir, "*.tmp")
                .Concat(Directory.GetFiles(tempDir, "*.temp"))
                .Concat(Directory.GetFiles(tempDir, "bosla_*"))
                .ToList();

            foreach (var file in tempFiles)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (lastWrite < cutoff)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not delete temp file {FilePath}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temp file cleanup scan");
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("Temporary file cleanup: deleted {Count} files", deletedCount);
        }

        return Task.CompletedTask;
    }
}