using Amazon;
using Amazon.S3;
using BoslaPlatform.Application.Features.RecordingAccess.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Infrastructure.Storage.Cloudflare;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Infrastructure.Storage.HealthChecks;
using BoslaPlatform.Infrastructure.Storage.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Generic storage provider (for non-recording features) ──────────
        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName));

        var provider = configuration.GetSection(StorageOptions.SectionName)["Provider"] ?? "CloudflareR2";

        switch (provider)
        {
            case "CloudflareR2":
            default:
                services.AddSingleton<IObjectStorage, CloudflareR2ObjectStorage>();
                break;
        }

        // ── Recording storage (Amazon S3 via Agora Cloud Recording) ────────
        // The S3 client reads storage credentials from AgoraSettings, which
        // are configured by Agora's Cloud Recording feature.
        services.AddScoped<IAmazonS3>(sp =>
        {
            var agoraSettings = sp.GetRequiredService<IOptions<AgoraSettings>>().Value;
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1
            };
            return new AmazonS3Client(
                agoraSettings.StorageAccessKey,
                agoraSettings.StorageSecretKey,
                config);
        });
        services.AddScoped<IRecordingStorage, AmazonS3RecordingStorage>();
        services.AddScoped<IRecordingStorageSettings, RecordingStorageSettings>();

        // ── Access & cache ─────────────────────────────────────────────────
        services.AddSingleton<PresignedUrlCache>();
        services.AddScoped<IRecordingAccessService, RecordingAccessService>();

        // ── Metrics (no-op by default — swap for OTel implementation) ──────
        services.AddSingleton<IRecordingMetrics, NoOpRecordingMetrics>();

        // ── Temporary file cleanup ─────────────────────────────────────────
        services.AddScoped<ITemporaryFileCleaner, DefaultTemporaryFileCleaner>();
        services.Configure<TemporaryFileCleanerOptions>(
            configuration.GetSection(TemporaryFileCleanerOptions.SectionName));
        services.AddHostedService<TemporaryFileCleanerService>();

        // ── Audit logging ──────────────────────────────────────────────────
        services.AddScoped<IRecordingAuditService, RecordingAuditService>();

        // ── Concurrency lock (process-level + EF RowVersion) ───────────────
        services.AddSingleton<IRecordingLock, OptimisticRecordingLock>();

        // ── Retention options (architecture stub — no deletion yet) ────────
        services.Configure<RecordingRetentionOptions>(
            configuration.GetSection(RecordingRetentionOptions.SectionName));

        // ── Health checks ──────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddCheck<CloudflareR2HealthCheck>(
                "cloudflare-r2",
                tags: ["storage", "readiness"])
            .AddCheck<StorageConfigurationHealthCheck>(
                "storage-configuration",
                tags: ["storage", "startup"]);

        return services;
    }
}
