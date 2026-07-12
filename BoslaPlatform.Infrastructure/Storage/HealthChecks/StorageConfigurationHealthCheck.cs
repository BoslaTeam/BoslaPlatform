using BoslaPlatform.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage.HealthChecks;

/// <summary>
/// Health check that validates all required <see cref="StorageOptions"/> fields
/// are configured. Does not make any network calls.
/// </summary>
internal sealed class StorageConfigurationHealthCheck : IHealthCheck
{
    private readonly StorageOptions _options;

    public StorageConfigurationHealthCheck(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
            failures.Add("StorageOptions:AccessKey is not configured.");

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            failures.Add("StorageOptions:SecretKey is not configured.");

        if (string.IsNullOrWhiteSpace(_options.ServiceUrl))
            failures.Add("StorageOptions:ServiceUrl is not configured.");

        if (string.IsNullOrWhiteSpace(_options.BucketName))
            failures.Add("StorageOptions:BucketName is not configured.");

        if (_options.PresignedUrlExpirationMinutes <= 0)
            failures.Add("StorageOptions:PresignedUrlExpirationMinutes must be > 0.");

        if (failures.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Storage configuration is incomplete.",
                null,
                new Dictionary<string, object>
                {
                    { "failures", string.Join("; ", failures) },
                    { "provider", _options.Provider }
                }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Storage configuration is valid.",
            new Dictionary<string, object>
            {
                { "provider", _options.Provider },
                { "bucket", _options.BucketName },
                { "presignedUrlExpirationMinutes", _options.PresignedUrlExpirationMinutes }
            }));
    }
}
