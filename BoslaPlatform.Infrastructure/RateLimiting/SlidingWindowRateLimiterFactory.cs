using BoslaPlatform.Infrastructure.Settings;
using System.Threading.RateLimiting;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Factory for constructing <see cref="RateLimitPartition{T}"/> instances configured with sliding window rate limiting.
/// Centralizes all inline construction of <see cref="SlidingWindowRateLimiterOptions"/> to eliminate duplication
/// and provide a single point of change for limiter configuration.
/// </summary>
public static class SlidingWindowRateLimiterFactory
{
    /// <summary>
    /// Creates a sliding window rate limiter partition for the given partition key and policy options.
    /// </summary>
    /// <param name="partitionKey">
    /// The partition key identifying the client (user ID, IP address, API key, or tenant).
    /// </param>
    /// <param name="options">
    /// The policy options specifying permit limit, window duration, and queue behavior.
    /// </param>
    /// <returns>
    /// A <see cref="RateLimitPartition{T}"/> configured with sliding window limits
    /// that can be registered with the rate limiter middleware.
    /// </returns>
    public static RateLimitPartition<string> Create(string partitionKey, RateLimitPolicyOptions options)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = options.QueueLimit
            });
    }
}
