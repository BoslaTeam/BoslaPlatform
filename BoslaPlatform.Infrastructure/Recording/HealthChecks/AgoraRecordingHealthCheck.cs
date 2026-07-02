using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Recording.HealthChecks
{
    internal sealed class AgoraRecordingHealthCheck : IHealthCheck
    {
        private readonly AgoraSettings _settings;

        public AgoraRecordingHealthCheck(IOptions<AgoraSettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken ct = default)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.AppId))
                failures.Add("AgoraSettings:AppId is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.CustomerId))
                failures.Add("AgoraSettings:CustomerId is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.CustomerSecret))
                failures.Add("AgoraSettings:CustomerSecret is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.CloudRecordingBaseUrl))
                failures.Add("AgoraSettings:CloudRecordingBaseUrl is not configured.");

            if (failures.Count > 0)
            {
                var description = "Agora Cloud Recording configuration is incomplete.";
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(description, null, new Dictionary<string, object>
                    {
                        { "failures", string.Join("; ", failures) }
                    }));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy("Agora Cloud Recording configuration is valid."));
        }
    }
}
