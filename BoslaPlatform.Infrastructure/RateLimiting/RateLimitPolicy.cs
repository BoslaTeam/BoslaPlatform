using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting policy that implements <see cref="IRateLimiterPolicy{T}"/> for string-partitioned rate limiting.
/// Stores policy metadata in <see cref="HttpContext.Items"/> for consumption by the global <c>OnRejected</c> handler
/// and automatically exempts infrastructure endpoints (health, Swagger, metrics) from rate limiting.
/// </summary>
public sealed class RateLimitPolicy : IRateLimiterPolicy<string>
{
    /// <summary>
    /// <see cref="HttpContext.Items"/> key under which the policy name is stored.
    /// </summary>
    public const string PolicyNameKey = "BoslaPlatform.RateLimit.PolicyName";

    /// <summary>
    /// <see cref="HttpContext.Items"/> key under which the resolved partition key is stored.
    /// </summary>
    public const string PartitionKeyKey = "BoslaPlatform.RateLimit.PartitionKey";

    /// <summary>
    /// <see cref="HttpContext.Items"/> key under which the configured permit limit is stored.
    /// </summary>
    public const string PermitLimitKey = "BoslaPlatform.RateLimit.PermitLimit";

    private static readonly string[] ExcludedPaths =
        ["/health", "/swagger", "/metrics"];

    private readonly string _policyName;
    private readonly RateLimitPolicyOptions _options;
    private readonly IRateLimitPartitionResolver _partitionResolver;

    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitPolicy"/>.
    /// </summary>
    /// <param name="policyName">The policy name used for identification in logs and headers.</param>
    /// <param name="options">The rate limit options (permit limit, window, queue behavior) for this policy.</param>
    /// <param name="partitionResolver">The resolver used to derive the partition key for each request.</param>
    public RateLimitPolicy(
        string policyName,
        RateLimitPolicyOptions options,
        IRateLimitPartitionResolver partitionResolver)
    {
        _policyName = policyName;
        _options = options;
        _partitionResolver = partitionResolver;
    }

    /// <inheritdoc />
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;

        if (IsExcludedPath(path))
        {
            return RateLimitPartition.GetNoLimiter(_policyName);
        }

        var partitionKey = _partitionResolver.Resolve(httpContext);

        httpContext.Items[PolicyNameKey] = _policyName;
        httpContext.Items[PartitionKeyKey] = partitionKey;
        httpContext.Items[PermitLimitKey] = _options.PermitLimit;

        return SlidingWindowRateLimiterFactory.Create(partitionKey, _options);
    }

    /// <summary>
    /// No per-policy rejection handler is provided.
    /// All rejection responses are handled centrally by the global <c>RateLimiterOptions.OnRejected</c> delegate,
    /// which reads policy metadata from <see cref="HttpContext.Items"/>.
    /// </summary>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    private static bool IsExcludedPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        foreach (var prefix in ExcludedPaths)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
