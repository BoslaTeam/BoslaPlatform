using BoslaPlatform.Application.Features.RecordingAccess.Services;
using BoslaPlatform.Application.Features.RecordingTransfer.Services;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Infrastructure.Storage.Cloudflare;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BoslaPlatform.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName));

        var provider = configuration.GetSection(StorageOptions.SectionName)["Provider"] ?? "CloudflareR2";

        switch (provider)
        {
            case "CloudflareR2":
                services.AddSingleton<IObjectStorage, CloudflareR2ObjectStorage>();
                break;
            default:
                services.AddSingleton<IObjectStorage, CloudflareR2ObjectStorage>();
                break;
        }

        services.AddScoped<RecordingTransferService>();
        services.AddScoped<IFileDownloader, HttpClientFileDownloader>();

        services.AddSingleton<PresignedUrlCache>();

        services.AddScoped<IRecordingAccessService, RecordingAccessService>();

        services.AddSingleton<IRecordingMetrics, NoOpRecordingMetrics>();

        services.AddScoped<ITemporaryFileCleaner, DefaultTemporaryFileCleaner>();

        services.Configure<TemporaryFileCleanerOptions>(
            configuration.GetSection(TemporaryFileCleanerOptions.SectionName));
        services.AddHostedService<TemporaryFileCleanerService>();

        return services;
    }
}