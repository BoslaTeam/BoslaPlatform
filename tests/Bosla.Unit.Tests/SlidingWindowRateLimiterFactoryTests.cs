using Xunit;
using BoslaPlatform.Infrastructure.RateLimiting;
using BoslaPlatform.Infrastructure.Settings;
using System.Threading.RateLimiting;

namespace Bosla.Unit.Tests;

public class SlidingWindowRateLimiterFactoryTests
{
    [Fact]
    public void Create_ReturnsSlidingWindowLimiterPartition()
    {
        var options = new RateLimitPolicyOptions
        {
            PermitLimit = 50,
            WindowSeconds = 30,
            QueueLimit = 2
        };

        var partition = SlidingWindowRateLimiterFactory.Create("user-123", options);

        Assert.IsAssignableFrom<RateLimitPartition<string>>(partition);
    }

    [Fact]
    public void Create_WithZeroQueueLimit_StillCreatesPartition()
    {
        var options = new RateLimitPolicyOptions
        {
            PermitLimit = 100,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        var partition = SlidingWindowRateLimiterFactory.Create("test", options);

        Assert.IsAssignableFrom<RateLimitPartition<string>>(partition);
    }

    [Fact]
    public void Create_WithQueueLimit_StillCreatesPartition()
    {
        var options = new RateLimitPolicyOptions
        {
            PermitLimit = 100,
            WindowSeconds = 60,
            QueueLimit = 5
        };

        var partition = SlidingWindowRateLimiterFactory.Create("test", options);

        Assert.IsAssignableFrom<RateLimitPartition<string>>(partition);
    }
}
