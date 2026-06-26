using Microsoft.SemanticKernel;
using BoslaPlatform.Infrastructure.AI.Qdrant;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

/// <summary>
/// Qdrant memory plugin for Semantic Kernel v1.77
/// Exposes memory store and retrieval functions
/// </summary>
public class QdrantMemoryPlugin
{
    private readonly QdrantClient _client;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantMemoryPlugin> _logger;

    public QdrantMemoryPlugin(QdrantClient client, IOptions<QdrantSettings> opts, ILogger<QdrantMemoryPlugin> logger)
    {
        _client = client;
        _settings = opts.Value;
        _logger = logger;
    }

    [KernelFunction("store_memory")]
    [Description("Store a memory vector in Qdrant")]
    public async Task<string> StoreMemoryAsync(
        [Description("The memory ID")] string id,
        [Description("The embedding vector as JSON")] string embeddingJson,
        [Description("Optional metadata")] string? metadata = null)
    {
        if (!Guid.TryParse(id, out var memoryId))
            return "Invalid memory ID format";

        var vec = JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
        var meta = metadata != null ? JsonSerializer.Deserialize<object>(metadata) : new { text = "memory" };

        await _client.UpsertPointAsync(memoryId, vec, meta);
        _logger.LogInformation("Stored memory {MemoryId}", memoryId);
        return $"Stored memory {memoryId}";
    }

    [KernelFunction("search_memory")]
    [Description("Search similar memories in Qdrant")]
    public async Task<string> SearchMemoryAsync(
        [Description("The query embedding as JSON")] string queryEmbeddingJson,
        [Description("Number of results (default 5)")] int topK = 5)
    {
        var vec = JsonSerializer.Deserialize<float[]>(queryEmbeddingJson) ?? Array.Empty<float>();
        var results = await _client.SearchAsync(vec, topK);

        var json = JsonSerializer.Serialize(results.Select(r => new { id = r.Id, score = r.Score }));
        _logger.LogInformation("Searched memory, found {Count} results", results.Count);
        return json;
    }
}
