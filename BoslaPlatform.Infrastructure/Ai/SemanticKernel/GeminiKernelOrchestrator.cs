using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using BoslaPlatform.Application.Interfaces.AI;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

/// <summary>
/// Factory and orchestrator for SK kernel with Gemini and Qdrant
/// </summary>
public class GeminiKernelOrchestrator
{
    private readonly Kernel _kernel;
    private readonly GeminiPlugin _geminiPlugin;
    private readonly QdrantMemoryPlugin _memoryPlugin;
    private readonly ILogger<GeminiKernelOrchestrator> _logger;

    public GeminiKernelOrchestrator(
        Kernel kernel,
        GeminiPlugin geminiPlugin,
        QdrantMemoryPlugin memoryPlugin,
        ILogger<GeminiKernelOrchestrator> logger)
    {
        _kernel = kernel;
        _geminiPlugin = geminiPlugin;
        _memoryPlugin = memoryPlugin;
        _logger = logger;

        // Register plugins
        _kernel.Plugins.AddFromObject(_geminiPlugin, "gemini");
        _kernel.Plugins.AddFromObject(_memoryPlugin, "qdrant_memory");
    }

    public Kernel Kernel => _kernel;

    /// <summary>
    /// Execute a simple prompt using the kernel
    /// </summary>
    public async Task<string> InvokeAsync(string promptText)
    {
        try
        {
            var result = await _kernel.InvokePromptAsync(promptText);
            return result?.ToString() ?? "No response";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking kernel prompt: {Prompt}", promptText);
            throw;
        }
    }

    /// <summary>
    /// Invoke a specific function by name
    /// </summary>
    public async Task<string> InvokeFunctionAsync(string pluginName, string functionName, Dictionary<string, object>? arguments = null)
    {
        try
        {
            var function = _kernel.Plugins[pluginName][functionName];
            var result = await _kernel.InvokeAsync(function, new KernelArguments(arguments ?? new Dictionary<string, object>()));
            return result?.ToString() ?? "No response";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking function {Plugin}.{Function}", pluginName, functionName);
            throw;
        }
    }
}
