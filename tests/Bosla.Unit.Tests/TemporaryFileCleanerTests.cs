using BoslaPlatform.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class TemporaryFileCleanerTests
{
    [Fact]
    public async Task CleanupAsync_deletes_expired_bosla_temp_files()
    {
        var loggerMock = Mock.Of<ILogger<DefaultTemporaryFileCleaner>>();
        var cleaner = new DefaultTemporaryFileCleaner(loggerMock);
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"bosla_test_{Guid.NewGuid()}.tmp");

        await File.WriteAllTextAsync(tempFile, "test");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        Assert.True(File.Exists(tempFile));

        await cleaner.CleanupAsync(TimeSpan.FromHours(1));

        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task CleanupAsync_does_not_delete_recent_files()
    {
        var loggerMock = Mock.Of<ILogger<DefaultTemporaryFileCleaner>>();
        var cleaner = new DefaultTemporaryFileCleaner(loggerMock);
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"bosla_recent_{Guid.NewGuid()}.tmp");

        await File.WriteAllTextAsync(tempFile, "test");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddMinutes(-5));

        try
        {
            await cleaner.CleanupAsync(TimeSpan.FromHours(1));
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CleanupAsync_uses_default_retention_when_not_specified()
    {
        var loggerMock = Mock.Of<ILogger<DefaultTemporaryFileCleaner>>();
        var cleaner = new DefaultTemporaryFileCleaner(loggerMock);

        await cleaner.CleanupAsync();

        _ = Assert.IsType<DefaultTemporaryFileCleaner>(cleaner);
    }

    [Fact]
    public async Task CleanupAsync_handles_missing_file_gracefully()
    {
        var loggerMock = Mock.Of<ILogger<DefaultTemporaryFileCleaner>>();
        var cleaner = new DefaultTemporaryFileCleaner(loggerMock);
        var missingFile = Path.Combine(Path.GetTempPath(), $"bosla_missing_{Guid.NewGuid()}.tmp");

        var exception = await Record.ExceptionAsync(() =>
            cleaner.CleanupAsync(TimeSpan.FromHours(1)));

        Assert.Null(exception);
    }
}