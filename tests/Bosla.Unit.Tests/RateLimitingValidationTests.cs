using Xunit;
using BoslaPlatform.Infrastructure.RateLimiting;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Bosla.Unit.Tests;

public class RateLimitingSettingsValidationTests
{
    private static RateLimitingSettingsValidation CreateValidator() => new();

    [Fact]
    public void Validate_WithValidSettings_ReturnsSuccess()
    {
        var settings = new RateLimitingSettings
        {
            Policies = new()
            {
                ["Test"] = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = 0 }
            }
        };

        var result = CreateValidator().Validate(null, settings);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WithEmptyPolicies_ReturnsSuccess()
    {
        var settings = new RateLimitingSettings { Policies = [] };

        var result = CreateValidator().Validate(null, settings);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithInvalidPermitLimit_ReturnsFail(int permitLimit)
    {
        var settings = new RateLimitingSettings
        {
            Policies = new()
            {
                ["Test"] = new() { PermitLimit = permitLimit, WindowSeconds = 60, QueueLimit = 0 }
            }
        };

        var result = CreateValidator().Validate(null, settings);

        Assert.NotEqual(ValidateOptionsResult.Success, result);
        Assert.False(result.Succeeded);
        Assert.Contains("PermitLimit", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidWindowSeconds_ReturnsFail(int windowSeconds)
    {
        var settings = new RateLimitingSettings
        {
            Policies = new()
            {
                ["Test"] = new() { PermitLimit = 10, WindowSeconds = windowSeconds, QueueLimit = 0 }
            }
        };

        var result = CreateValidator().Validate(null, settings);

        Assert.NotEqual(ValidateOptionsResult.Success, result);
        Assert.False(result.Succeeded);
        Assert.Contains("WindowSeconds", result.FailureMessage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNegativeQueueLimit_ReturnsFail(int queueLimit)
    {
        var settings = new RateLimitingSettings
        {
            Policies = new()
            {
                ["Test"] = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = queueLimit }
            }
        };

        var result = CreateValidator().Validate(null, settings);

        Assert.NotEqual(ValidateOptionsResult.Success, result);
        Assert.False(result.Succeeded);
        Assert.Contains("QueueLimit", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithMultiplePolicies_ValidatesAll()
    {
        var settings = new RateLimitingSettings
        {
            Policies = new()
            {
                ["Good"] = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = 0 },
                ["BadPermit"] = new() { PermitLimit = 0, WindowSeconds = 60, QueueLimit = 0 },
                ["BadWindow"] = new() { PermitLimit = 10, WindowSeconds = -1, QueueLimit = 0 }
            }
        };

        var result = CreateValidator().Validate(null, settings);

        Assert.NotEqual(ValidateOptionsResult.Success, result);
        Assert.False(result.Succeeded);
        Assert.Contains("BadPermit", result.FailureMessage);
        Assert.Contains("BadWindow", result.FailureMessage);
    }
}
