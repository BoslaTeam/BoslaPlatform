using BoslaPlatform.Application.Interfaces.Storage;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Storage;

public sealed class DefaultTemporaryFileCleaner : ITemporaryFileCleaner
{
    private readonly ILogger<DefaultTemporaryFileCleaner> _logger;

    // Maximum single-file size to attempt deletion (100 MB guard against accidentally
    // targeting non-temp large files that happen to share the bosla_ prefix).
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;

    public DefaultTemporaryFileCleaner(ILogger<DefaultTemporaryFileCleaner> logger)
    {
        _logger = logger;
    }

    public Task CleanupAsync(TimeSpan? retention = null, CancellationToken ct = default)
    {
        var retentionTime = retention ?? TimeSpan.FromHours(1);
        var tempDir = Path.GetTempPath();
        var cutoff = DateTime.UtcNow.Subtract(retentionTime);

        int orphanCount = 0;      // bosla_* download files without a corresponding transfer
        int remnantCount = 0;     // .tmp / .temp files from failed downloads
        int skippedCount = 0;     // files that could not be deleted
        long reclaimedBytes = 0;

        _logger.LogDebug(
            "Temp file cleanup started. Dir={Dir}, Retention={Retention}, Cutoff={Cutoff}",
            tempDir, retentionTime, cutoff);

        try
        {
            // Category 1: Bosla recording download orphans (bosla_* prefix)
            var boslaFiles = Directory.GetFiles(tempDir, "bosla_*");
            foreach (var file in boslaFiles)
            {
                if (ct.IsCancellationRequested) break;
                if (TryDeleteIfExpired(file, cutoff, ref reclaimedBytes))
                    orphanCount++;
                else if (File.Exists(file))
                    skippedCount++;
            }

            // Category 2: Generic temp remnants from failed/interrupted downloads
            var tmpFiles = Directory.GetFiles(tempDir, "*.tmp")
                .Concat(Directory.GetFiles(tempDir, "*.temp"));

            foreach (var file in tmpFiles)
            {
                if (ct.IsCancellationRequested) break;
                if (TryDeleteIfExpired(file, cutoff, ref reclaimedBytes))
                    remnantCount++;
                else if (File.Exists(file))
                    skippedCount++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temp file cleanup scan in {Dir}", tempDir);
        }

        var total = orphanCount + remnantCount;
        if (total > 0)
        {
            _logger.LogInformation(
                "Temp file cleanup completed: Orphans={Orphans}, Remnants={Remnants}, Skipped={Skipped}, Reclaimed={ReclaimedMB:F2}MB",
                orphanCount, remnantCount, skippedCount, reclaimedBytes / (1024.0 * 1024));
        }
        else
        {
            _logger.LogDebug(
                "Temp file cleanup: nothing to clean (Skipped={Skipped})", skippedCount);
        }

        return Task.CompletedTask;
    }

    private bool TryDeleteIfExpired(string filePath, DateTime cutoff, ref long reclaimedBytes)
    {
        try
        {
            var info = new FileInfo(filePath);

            if (!info.Exists) return false;

            // Skip files that are newer than the cutoff — they may be in active use.
            if (info.LastWriteTimeUtc >= cutoff) return false;

            // Guard against accidentally deleting large non-temp files.
            if (info.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning(
                    "Temp cleanup skipping oversized file {Path} ({SizeMB:F1}MB)",
                    filePath, info.Length / (1024.0 * 1024));
                return false;
            }

            reclaimedBytes += info.Length;
            File.Delete(filePath);

            _logger.LogDebug("Deleted temp file {Path} (age={LastWrite})", filePath, info.LastWriteTimeUtc);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not delete temp file {FilePath}", filePath);
            return false;
        }
    }
}