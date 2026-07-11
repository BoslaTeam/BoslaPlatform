using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Extension methods for configuring the application's rate limiting infrastructure.
/// Registers strongly typed options, partition resolution, per-policy rate limiters,
/// startup validation, a centralized rejection handler, and rate limit response headers.
/// </summary>
public static class ConfigureRateLimitingExtensions
{
    private const string ProblemDetailsType = "https://tools.ietf.org/html/rfc6585#section-4";
    private const string ProblemDetailsTitle = "Too Many Requests";
    private const string ProblemDetailsDetail = "You have exceeded the allowed number of requests. Please try again later.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] DefinedPolicyNames =
    [
        RateLimitPolicyNames.Anonymous,
        RateLimitPolicyNames.Authenticated,
        RateLimitPolicyNames.Sensitive,
        RateLimitPolicyNames.Upload,
        RateLimitPolicyNames.AI,
        RateLimitPolicyNames.PublicSearch,
    ];

    /// <summary>
    /// Registers rate limiting services using the Options pattern for strongly typed configuration,
    /// a pluggable partition resolver, per-policy <see cref="IRateLimiterPolicy{T}"/> instances,
    /// startup validation via <see cref="IValidateOptions{T}"/>, and a centralized <c>OnRejected</c> handler
    /// that emits structured logs and rate limit response headers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration containing the "RateLimiting" section.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimitingPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitingSettings>(
            configuration.GetSection("RateLimiting"));

        services.AddSingleton<IValidateOptions<RateLimitingSettings>, RateLimitingSettingsValidation>();

        services.AddSingleton<IRateLimitPartitionResolver, DefaultRateLimitPartitionResolver>();

        services.AddRateLimiter();

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>, IRateLimitPartitionResolver, ILoggerFactory>(
                (options, settings, resolver, loggerFactory) =>
                {
                    var rateLimitSettings = settings.Value;
                    var policies = rateLimitSettings.Policies
                        ?? new Dictionary<string, RateLimitPolicyOptions>();

                    foreach (var policyName in DefinedPolicyNames)
                    {
                        var policyOptions = policies.GetValueOrDefault(policyName)
                            ?? new RateLimitPolicyOptions();

                        options.AddPolicy(policyName, new RateLimitPolicy(policyName, policyOptions, resolver));
                    }

                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    var logger = loggerFactory.CreateLogger("BoslaPlatform.Infrastructure.RateLimiting");

                    options.OnRejected = (context, ct) => WriteRejectionResponse(context, ct, logger);
                });

        return services;
    }

    private static async ValueTask WriteRejectionResponse(
        OnRejectedContext context,
        CancellationToken ct,
        ILogger logger)
    {
        var httpContext = context.HttpContext;

        var policyName = httpContext.Items[RateLimitPolicy.PolicyNameKey] as string ?? "(unknown)";
        var partitionKey = httpContext.Items[RateLimitPolicy.PartitionKeyKey] as string ?? "(unknown)";
        var permitLimit = httpContext.Items[RateLimitPolicy.PermitLimitKey] is int limit ? limit : 0;

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? "(anonymous)";

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "(unknown)";
        var method = httpContext.Request.Method;
        var endpoint = httpContext.Request.Path + httpContext.Request.QueryString;
        var correlationId = httpContext.TraceIdentifier;

        var retryAfter = GetRetryAfter(context);

        // TODO: Emit rate limit metrics (meter counters) via System.Diagnostics.Metrics
        //   - rate_limit_rejected_total (tagged by policy, endpoint, userId)
        //   - rate_limit_retry_after_seconds (histogram)

        logger.LogWarning(
            "Rate limit exceeded. Method={Method}, Endpoint={Endpoint}, Policy={Policy}, PartitionKey={PartitionKey}, UserId={UserId}, IP={IP}, RetryAfter={RetryAfter}, CorrelationId={CorrelationId}",
            method, endpoint, policyName, partitionKey, userId, ip, retryAfter?.TotalSeconds ?? 0, correlationId);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";

        httpContext.Response.Headers["X-RateLimit-Limit"] = permitLimit.ToString();
        httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";

        if (retryAfter.HasValue)
        {
            httpContext.Response.Headers.RetryAfter =
                ((long)retryAfter.Value.TotalSeconds).ToString();
        }

        var problemDetails = new ProblemDetails
        {
            Type = ProblemDetailsType,
            Title = ProblemDetailsTitle,
            Status = StatusCodes.Status429TooManyRequests,
            Detail = ProblemDetailsDetail
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, JsonOptions, ct);
    }

    private static TimeSpan? GetRetryAfter(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            return retryAfter;
        }

        return null;
    }
}
