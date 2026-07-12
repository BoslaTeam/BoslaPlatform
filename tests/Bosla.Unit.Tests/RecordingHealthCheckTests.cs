using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bosla.Unit.Tests;

public class RecordingHealthCheckTests
{
    // ── StorageConfigurationHealthCheck ───────────────────────────────────────

    [Fact]
    public async Task StorageConfigurationHealthCheck_Healthy_when_all_fields_set()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions
            {
                Provider = "CloudflareR2",
                AccessKey = "AKID",
                SecretKey = "secret",
                ServiceUrl = "https://account.r2.cloudflarestorage.com",
                BucketName = "bosla-recordings",
                PresignedUrlExpirationMinutes = 15
            });

        var check = new BoslaPlatform.Infrastructure.Storage.HealthChecks
            .StorageConfigurationHealthCheck(options);

        var result = await check.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            result.Status);
        Assert.Contains("CloudflareR2", result.Data["provider"].ToString());
    }

    [Theory]
    [InlineData("", "secret", "https://r2.example.com", "bucket")]
    [InlineData("key", "", "https://r2.example.com", "bucket")]
    [InlineData("key", "secret", "", "bucket")]
    [InlineData("key", "secret", "https://r2.example.com", "")]
    public async Task StorageConfigurationHealthCheck_Unhealthy_when_field_missing(
        string access, string secret, string url, string bucket)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions
            {
                AccessKey = access,
                SecretKey = secret,
                ServiceUrl = url,
                BucketName = bucket,
                PresignedUrlExpirationMinutes = 15
            });

        var check = new BoslaPlatform.Infrastructure.Storage.HealthChecks
            .StorageConfigurationHealthCheck(options);

        var result = await check.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            result.Status);
        Assert.True(result.Data.ContainsKey("failures"));
    }

    // ── CloudflareR2HealthCheck ───────────────────────────────────────────────

    [Fact]
    public async Task CloudflareR2HealthCheck_Healthy_when_storage_responds_success()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.ExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<bool>)false); // 404-like: not found but reachable

        var options = Microsoft.Extensions.Options.Options.Create(
            new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions
            {
                AccessKey = "k", SecretKey = "s",
                ServiceUrl = "https://r2.example.com",
                BucketName = "bucket"
            });

        var check = new BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck(
            storageMock.Object,
            options,
            Mock.Of<ILogger<BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck>>());

        var result = await check.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            result.Status);
    }


    [Fact]
    public async Task CloudflareR2HealthCheck_Unhealthy_when_storage_throws()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.ExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var options = Microsoft.Extensions.Options.Options.Create(
            new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions
            {
                AccessKey = "k", SecretKey = "s",
                ServiceUrl = "https://r2.example.com",
                BucketName = "bucket"
            });

        var check = new BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck(
            storageMock.Object,
            options,
            Mock.Of<ILogger<BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck>>());

        var result = await check.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            result.Status);
    }

    [Fact]
    public async Task CloudflareR2HealthCheck_Degraded_when_storage_returns_error()
    {
        var storageMock = new Mock<IObjectStorage>();
        storageMock.Setup(s => s.ExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("R2.Error", "Access denied"));

        var options = Microsoft.Extensions.Options.Options.Create(
            new BoslaPlatform.Infrastructure.Storage.Configuration.StorageOptions
            {
                AccessKey = "k", SecretKey = "s",
                ServiceUrl = "https://r2.example.com",
                BucketName = "bucket"
            });

        var check = new BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck(
            storageMock.Object,
            options,
            Mock.Of<ILogger<BoslaPlatform.Infrastructure.Storage.HealthChecks.CloudflareR2HealthCheck>>());

        var result = await check.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            result.Status);
        Assert.True(result.Data.ContainsKey("error"));
    }
}
