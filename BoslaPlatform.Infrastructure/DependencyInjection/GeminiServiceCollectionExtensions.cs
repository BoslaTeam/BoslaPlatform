using BoslaPlatform.Infrastructure.AI.Gemini;
using BoslaPlatform.Infrastructure.AI.Qdrant;
using BoslaPlatform.Infrastructure.AI.SemanticKernel;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace BoslaPlatform.Infrastructure.DependencyInjection;

public static class GeminiServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAI(this IServiceCollection services)
    {
        services.Configure<GeminiSettings>(options => { });
        services.AddHttpClient("gemini");
        services.AddSingleton<GeminiHttpClient>();
        services.AddSingleton<EmbeddingBatcher>();
        services.AddScoped<GeminiChatService>();
        services.AddScoped<GeminiEmbeddingService>();
        services.AddScoped<QdrantMemoryAdapter>();
        services.AddScoped<GeminiKernelAdapter>();
        return services;
    }
}
