using BoslaPlatform.Application.Features.RecordingAccess.Services;
using BoslaPlatform.Application.Features.RecordingTransfer.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Infrastructure.BackgroundJobs;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;
using BoslaPlatform.Infrastructure.Storage.Cloudflare;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Infrastructure.Storage.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace BoslaPlatform.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Storage provider ───────────────────────────────────────────────
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

        // ── Recording pipeline ─────────────────────────────────────────────
        services.AddScoped<RecordingTransferService>();
        services.AddScoped<IFileDownloader, HttpClientFileDownloader>();
        services.AddScoped<IAgoraRecordingDownloader, AgoraRecordingDownloader>();
        services.AddScoped<IRecordingStorageSettings, RecordingStorageSettings>();

        services.AddHttpClient(AgoraRecordingDownloader.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10);
            })
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

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

        // ── Integrity verification ─────────────────────────────────────────
        services.AddScoped<RecordingIntegrityVerifier>();

        // ── Concurrency lock (process-level + EF RowVersion) ───────────────
        services.AddSingleton<IRecordingLock, OptimisticRecordingLock>();

        // ── Reconciliation background service ──────────────────────────────
        services.Configure<RecordingReconciliationOptions>(
            configuration.GetSection(RecordingReconciliationOptions.SectionName));
        services.AddHostedService<RecordingReconciliationService>();

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
