using Microsoft.SemanticKernel;
using BoslaPlatform.Infrastructure.AI.Qdrant;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.VectorData;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

/// <summary>
/// Qdrant memory plugin for Semantic Kernel v1.77
/// Uses the custom QdrantMemoryAdapter/QdrantClient to ensure consistent logging and error handling
/// </summary>
public class QdrantMemoryPlugin
{
    private readonly QdrantMemoryAdapter _memoryAdapter;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantMemoryPlugin> _logger;

    public QdrantMemoryPlugin(QdrantMemoryAdapter memoryAdapter, IOptions<QdrantSettings> opts, ILogger<QdrantMemoryPlugin> logger)
    {
        _memoryAdapter = memoryAdapter;
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

        try
        {
            var vec = JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
            if (vec.Length == 0)
            {
                _logger.LogError("Failed to deserialize embedding vector for memory {MemoryId}", memoryId);
                return $"Failed to deserialize embedding vector for {memoryId}";
            }

            var meta = metadata != null ? JsonSerializer.Deserialize<object>(metadata) : new { text = "memory" };
            await _memoryAdapter.UpsertAsync(memoryId, vec, meta ?? new { });
            _logger.LogInformation("? Stored memory {MemoryId} via QdrantMemoryAdapter", memoryId);
            return $"Stored memory {memoryId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store memory {MemoryId}", memoryId);
            return $"Failed to store memory {memoryId}: {ex.Message}";
        }
    }

    [KernelFunction("search_memory")]
    [Description("Search similar memories in Qdrant")]
    public async Task<string> SearchMemoryAsync(
        [Description("The query embedding as JSON")] string queryEmbeddingJson,
        [Description("Number of results (default 5)")] int topK = 5)
    {
        try
        {
            var vec = JsonSerializer.Deserialize<float[]>(queryEmbeddingJson) ?? Array.Empty<float>();
            if (vec.Length == 0)
            {
                _logger.LogError("Failed to deserialize query embedding vector");
                return "[]";
            }

            var results = await _memoryAdapter.SearchAsync(vec, topK);
            var json = JsonSerializer.Serialize(results.Select(r => new { id = r.Id.ToString(), score = r.Score }));
            _logger.LogInformation("? Searched memory, found {Count} results via QdrantMemoryAdapter", results.Count);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search memory");
            return "[]";
        }
    }
}
