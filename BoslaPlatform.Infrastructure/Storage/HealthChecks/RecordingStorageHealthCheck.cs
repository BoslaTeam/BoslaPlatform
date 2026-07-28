using Amazon.S3;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage.HealthChecks;

/// <summary>
/// Validates the Amazon S3 bucket that Agora Cloud Recording uploads to.
///
/// The recorder uploads straight from Agora's servers to S3, so a wrong bucket
/// name, region, or key pair produces no error anywhere in our own request path:
/// Stop returns 200 and the bucket simply stays empty. This check reaches the
/// bucket at startup so that misconfiguration fails loudly instead of silently
/// losing every recording.
/// </summary>
internal sealed class RecordingStorageHealthCheck : IHealthCheck
{
    private readonly AgoraSettings _settings;
    private readonly IAmazonS3 _s3;

    public RecordingStorageHealthCheck(IOptions<AgoraSettings> options, IAmazonS3 s3)
    {
        _settings = options.Value;
        _s3 = s3;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.StorageBucket))
            failures.Add("AgoraSettings:StorageBucket is not configured.");

        if (string.IsNullOrWhiteSpace(_settings.StorageAccessKey))
            failures.Add("AgoraSettings:StorageAccessKey is not configured.");

        if (string.IsNullOrWhiteSpace(_settings.StorageSecretKey))
            failures.Add("AgoraSettings:StorageSecretKey is not configured.");

        if (failures.Count > 0)
        {
            return HealthCheckResult.Unhealthy(
                "Agora recording storage configuration is incomplete.",
                data: new Dictionary<string, object>
                {
                    { "failures", string.Join("; ", failures) }
                });
        }

        try
        {
            var location = await _s3.GetBucketLocationAsync(_settings.StorageBucket, ct);

            // S3 reports us-east-1 as an empty location constraint.
            var actualRegion = string.IsNullOrEmpty(location.Location?.Value)
                ? "us-east-1"
                : location.Location.Value;

            if (!string.Equals(actualRegion, _settings.StorageRegionSystemName, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Unhealthy(
                    $"Bucket '{_settings.StorageBucket}' lives in '{actualRegion}' but is configured as " +
                    $"'{_settings.StorageRegionSystemName}'. Agora will fail to upload recordings.",
                    data: new Dictionary<string, object>
                    {
                        { "bucket", _settings.StorageBucket },
                        { "actualRegion", actualRegion },
                        { "configuredRegion", _settings.StorageRegionSystemName },
                        { "agoraRegionCode", _settings.StorageRegion }
                    });
            }

            return HealthCheckResult.Healthy(
                "Agora recording storage is reachable.",
                new Dictionary<string, object>
                {
                    { "bucket", _settings.StorageBucket },
                    { "region", actualRegion },
                    { "agoraRegionCode", _settings.StorageRegion },
                    { "vendor", _settings.StorageVendor }
                });
        }
        catch (AmazonS3Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Cannot reach recording bucket '{_settings.StorageBucket}': {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    { "bucket", _settings.StorageBucket },
                    { "statusCode", (int)ex.StatusCode },
                    { "errorCode", ex.ErrorCode ?? string.Empty }
                });
        }
    }
}
