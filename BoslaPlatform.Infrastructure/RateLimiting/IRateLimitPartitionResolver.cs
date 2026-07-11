using Microsoft.AspNetCore.Http;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Resolves the rate limit partition key for a given HTTP request.
/// Partition keys isolate rate limit counters per distinct client identity (user, IP, API key, tenant, or admin role).
/// Concrete implementations determine the resolution strategy.
/// </summary>
public interface IRateLimitPartitionResolver
{
    /// <summary>
    /// Resolves the partition key from the current HTTP context.
    /// The returned value is used to isolate rate limit counters so that one client's usage does not affect another's.
    /// </summary>
    /// <param name="httpContext">The current HTTP request context.</param>
    /// <returns>A string partition key that uniquely identifies the client for rate limiting purposes.</returns>
    string Resolve(HttpContext httpContext);
}
