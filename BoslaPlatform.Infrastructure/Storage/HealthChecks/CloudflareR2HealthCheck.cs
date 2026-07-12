using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage.HealthChecks;

/// <summary>
/// Health check that verifies Cloudflare R2 connectivity by issuing a metadata
/// request for a known test key. Reports Healthy if the request completes
/// (even if the key does not exist — 404 still proves connectivity).
/// Reports Degraded/Unhealthy on connection failures or misconfigurations.
/// </summary>
internal sealed class CloudflareR2HealthCheck : IHealthCheck
{
    private const string TestKey = "__healthcheck__";

    private readonly IObjectStorage _storage;
    private readonly StorageOptions _options;
    private readonly ILogger<CloudflareR2HealthCheck> _logger;

    public CloudflareR2HealthCheck(
        IObjectStorage storage,
        IOptions<StorageOptions> options,
        ILogger<CloudflareR2HealthCheck> logger)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>
        {
            { "provider", "CloudflareR2" },
            { "bucket", _options.BucketName },
            { "serviceUrl", _options.ServiceUrl }
        };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // ExistsAsync returns Success(false) for 404 — that still proves connectivity.
            var result = await _storage.ExistsAsync(_options.BucketName, TestKey, cts.Token);

            if (result.IsError)
            {
                var error = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Cloudflare R2 health check returned error: {Error}", error);
                data["error"] = error;
                return HealthCheckResult.Degraded(
                    "Cloudflare R2 returned an error response.", null, data);
            }

            return HealthCheckResult.Healthy("Cloudflare R2 is reachable.", data);
        }
        catch (OperationCanceledException)
        {
            data["error"] = "Timed out after 5 seconds";
            return HealthCheckResult.Unhealthy(
                "Cloudflare R2 health check timed out.", null, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudflare R2 health check failed with exception");
            data["error"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                "Cloudflare R2 is unreachable.", ex, data);
        }
    }
}
