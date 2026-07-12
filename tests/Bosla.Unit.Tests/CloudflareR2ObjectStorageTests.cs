using BoslaPlatform.Infrastructure.Storage.Cloudflare;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class CloudflareR2ObjectStorageTests
{
    private static StorageOptions CreateValidOptions()
    {
        return new StorageOptions
        {
            AccessKey = "test-key",
            SecretKey = "test-secret",
            ServiceUrl = "https://test.r2.cloudflarestorage.com",
            BucketName = "test-bucket",
            Provider = "CloudflareR2",
            PresignedUrlExpirationMinutes = 15,
            MaxRetryAttempts = 3,
            RetryBaseDelaySeconds = 2
        };
    }

    [Fact]
    public void Constructor_succeeds_with_valid_options()
    {
        var options = CreateValidOptions();
        var loggerMock = Mock.Of<ILogger<CloudflareR2ObjectStorage>>();

        var storage = new CloudflareR2ObjectStorage(
            Options.Create(options),
            loggerMock);

        Assert.NotNull(storage);
    }

    [Fact]
    public void Dispose_does_not_throw()
    {
        var options = CreateValidOptions();
        var loggerMock = Mock.Of<ILogger<CloudflareR2ObjectStorage>>();
        var storage = new CloudflareR2ObjectStorage(
            Options.Create(options),
            loggerMock);

        var exception = Record.Exception(() => storage.Dispose());
        Assert.Null(exception);
    }
}