using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using BoslaPlatform.Infrastructure.AI.SemanticKernel;
using Xunit;

namespace BoslaPlatform.Tests.Infrastructure.AI.SemanticKernel;

public class SemanticKernelPluginTests
{
    [Fact]
    public void AddSemanticKernelForGemini_Should_Register_All_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Mock dependencies (simplified)
        // In a real test, you'd mock IConfiguration and set up Gemini settings

        // Act
        services.AddSemanticKernelForGemini();

        // Assert
        var provider = services.BuildServiceProvider();

        // Verify Kernel is registered
        var kernel = provider.GetService<Kernel>();
        Assert.NotNull(kernel);

        // Verify plugins are registered
        var geminiPlugin = provider.GetService<GeminiPlugin>();
        Assert.NotNull(geminiPlugin);

        var memoryPlugin = provider.GetService<QdrantMemoryPlugin>();
        Assert.NotNull(memoryPlugin);

        // Verify orchestrator is registered
        var orchestrator = provider.GetService<GeminiKernelOrchestrator>();
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void GeminiKernelOrchestrator_Should_Register_Plugins_In_Kernel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSemanticKernelForGemini();

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetService<GeminiKernelOrchestrator>();

        // Act
        var kernel = orchestrator!.Kernel;

        // Assert
        Assert.NotNull(kernel);
        Assert.NotEmpty(kernel.Plugins);

        // Verify plugins are accessible
        Assert.Contains("gemini", kernel.Plugins.Select(p => p.Name));
        Assert.Contains("qdrant_memory", kernel.Plugins.Select(p => p.Name));
    }

    [Fact]
    public void GeminiPlugin_Should_Expose_KernelFunctions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSemanticKernelForGemini();

        var provider = services.BuildServiceProvider();
        var kernel = provider.GetService<Kernel>();

        // Act & Assert
        Assert.NotNull(kernel);
        var geminiPlugin = kernel!.Plugins["gemini"];
        Assert.NotNull(geminiPlugin);

        // Check for expected functions
        Assert.Contains("ask_gemini", geminiPlugin.Select(f => f.Name));
        Assert.Contains("embed_text", geminiPlugin.Select(f => f.Name));
    }

    [Fact]
    public void QdrantMemoryPlugin_Should_Expose_KernelFunctions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSemanticKernelForGemini();

        var provider = services.BuildServiceProvider();
        var kernel = provider.GetService<Kernel>();

        // Act & Assert
        Assert.NotNull(kernel);
        var memoryPlugin = kernel!.Plugins["qdrant_memory"];
        Assert.NotNull(memoryPlugin);

        // Check for expected functions
        Assert.Contains("store_memory", memoryPlugin.Select(f => f.Name));
        Assert.Contains("search_memory", memoryPlugin.Select(f => f.Name));
    }
}
