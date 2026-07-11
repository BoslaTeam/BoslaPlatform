using BoslaPlatform.Infrastructure.Data.Outbox;
using Xunit;

namespace Bosla.Unit.Tests;

public class OutboxRetryCalculatorTests
{
    private static readonly OutboxRetryOptions DefaultOptions = new()
    {
        BaseDelaySeconds = 30,
        MaxDelayMinutes = 30,
        MaxRetryCount = 5
    };

    public static TheoryData<int, double> ExponentialGrowthData => new()
    {
        { 1, 30.0 },    // 30 × 2^0
        { 2, 60.0 },    // 30 × 2^1
        { 3, 120.0 },   // 30 × 2^2
        { 4, 240.0 },   // 30 × 2^3
        { 5, 480.0 },   // 30 × 2^4
    };

    [Theory]
    [MemberData(nameof(ExponentialGrowthData))]
    public void CalculateDelay_returns_exponential_backoff(int retryCount, double expected)
    {
        var delay = OutboxRetryCalculator.CalculateDelay(retryCount, DefaultOptions);

        Assert.Equal(expected, delay);
    }

    [Fact]
    public void CalculateDelay_caps_at_max_delay_minutes()
    {
        var options = new OutboxRetryOptions
        {
            BaseDelaySeconds = 30,
            MaxDelayMinutes = 1,  // 60 seconds cap
            MaxRetryCount = 10
        };

        var delay = OutboxRetryCalculator.CalculateDelay(5, options);

        Assert.Equal(60.0, delay);
    }

    [Fact]
    public void CalculateDelay_caps_at_max_delay_when_exponential_exceeds()
    {
        var delay = OutboxRetryCalculator.CalculateDelay(7, DefaultOptions);

        Assert.Equal(1800.0, delay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CalculateDelay_throws_for_invalid_retry_count(int invalidRetryCount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => OutboxRetryCalculator.CalculateDelay(invalidRetryCount, DefaultOptions));

        Assert.Contains("Retry count must be 1 or greater", ex.Message);
    }
}
