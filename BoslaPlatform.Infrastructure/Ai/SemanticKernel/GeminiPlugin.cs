using Microsoft.SemanticKernel;
using System.ComponentModel;
using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

/// <summary>
/// Gemini plugin for Semantic Kernel v1.77 that exposes chat and embedding functions
/// </summary>
public class GeminiPlugin
{
    private readonly IChatService _chat;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<GeminiPlugin> _logger;

    public GeminiPlugin(IChatService chat, IEmbeddingService embeddings, ILogger<GeminiPlugin> logger)
    {
        _chat = chat;
        _embeddings = embeddings;
        _logger = logger;
    }

    [KernelFunction("ask_gemini")]
    [Description("Ask Gemini a question and get a response")]
    public async Task<string> AskAsync([Description("The question to ask")] string prompt)
    {
        _logger.LogInformation("GeminiPlugin.Ask: {Prompt}", prompt);
        return await _chat.ChatAsync(prompt);
    }

    [KernelFunction("embed_text")]
    [Description("Generate embeddings for text")]
    public async Task<string> EmbedAsync([Description("Text to embed")] string text)
    {
        _logger.LogInformation("GeminiPlugin.Embed: {Text}", text);
        return await _embeddings.CreateEmbeddingAsync(text);
    }
}
