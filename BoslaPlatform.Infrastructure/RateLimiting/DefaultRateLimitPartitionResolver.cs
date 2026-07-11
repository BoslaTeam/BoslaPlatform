using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BoslaPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Default partition key resolver that distinguishes authenticated users from anonymous visitors.
/// Authenticated requests are partitioned by the user's unique identifier (<c>NameIdentifier</c> or <c>sub</c> claim);
/// anonymous requests are partitioned by the client's remote IP address.
/// </summary>
/// <remarks>
/// Future extension points:
/// <list type="bullet">
///   <item><description>API key authentication — check <c>X-Api-Key</c> header and resolve to a tenant/client ID.</description></item>
///   <item><description>Multi-tenant support — derive tenant from JWT claim or host header.</description></item>
///   <item><description>Admin overrides — apply separate partitions or exemptions for admin roles.</description></item>
/// </list>
/// Implement a decorator or replace the registration with a custom <see cref="IRateLimitPartitionResolver"/> to add these extensions.
/// </remarks>
public sealed class DefaultRateLimitPartitionResolver : IRateLimitPartitionResolver
{
    /// <inheritdoc />
    public string Resolve(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub")
                ?? "unknown-user";
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
    }
}
