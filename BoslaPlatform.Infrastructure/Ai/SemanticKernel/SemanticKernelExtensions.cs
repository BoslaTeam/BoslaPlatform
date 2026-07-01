using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernelForGemini(this IServiceCollection services)
    {
        // Register Kernel
        services.AddScoped<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();
            return builder.Build();
        });

        // Register the SK Qdrant memory service wrapper
        services.AddScoped<QdrantMemoryService>();

        // Register Gemini and Qdrant plugins
        services.AddScoped<GeminiPlugin>();
        services.AddScoped<QdrantMemoryPlugin>();

        // Register orchestrator (ties everything together)
        services.AddScoped<GeminiKernelOrchestrator>();

        return services;
    }
}
