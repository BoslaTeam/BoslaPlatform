using BoslaPlatform.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

// Minimal, dependency-light SK adapter: provide simple wrappers instead of full SK package
public class GeminiKernelAdapter
{
    private readonly IChatService _chat;
    private readonly IEmbeddingService _embeddings;
    private readonly QdrantMemoryAdapter _memory;
    private readonly ILogger<GeminiKernelAdapter> _logger;

    public GeminiKernelAdapter(IChatService chat, IEmbeddingService embeddings, QdrantMemoryAdapter memory, ILogger<GeminiKernelAdapter> logger)
    {
        _chat = chat;
        _embeddings = embeddings;
        _memory = memory;
        _logger = logger;
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // simple chain: embed prompt, store to memory (optional), then chat
        try
        {
            var embJson = await _embeddings.CreateEmbeddingAsync(prompt, cancellationToken);
            var vec = System.Text.Json.JsonSerializer.Deserialize<float[]>(embJson) ?? Array.Empty<float>();
            await _memory.UpsertAsync(Guid.NewGuid(), vec, new { text = prompt }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding or memory store failed");
        }

        return await _chat.ChatAsync(prompt, cancellationToken);
    }
}
