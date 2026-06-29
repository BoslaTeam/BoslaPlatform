using BoslaPlatform.Infrastructure.AI.Qdrant;
using Microsoft.Extensions.Options;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using BoslaPlatform.Application.Interfaces.AI;
using System.Text.Json;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.VectorData;
using System.Dynamic;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

public class QdrantSearchResult
{
    public Guid Id { get; set; }
    public float Score { get; set; }
    public string? Text { get; set; }
}

public class QdrantMemoryService
{
    private readonly global::Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore _qdrantStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantMemoryService> _logger;


    public QdrantMemoryService(
        global::Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore qdrantStore,
        IEmbeddingService embeddingService,
        IOptions<QdrantSettings> opts,
        ILogger<QdrantMemoryService> logger)
    {
        _qdrantStore = qdrantStore ?? throw new ArgumentNullException(nameof(qdrantStore));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _settings = opts.Value;
        _logger = logger;
    }

    /// <summary>
    /// Store a text entry with Gemini embeddings in Qdrant
    /// </summary>
    public async Task<string> StoreTextAsync(
        string text,
        string? id = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddingJson = await _embeddingService.CreateEmbeddingAsync(text, cancellationToken);
            var embedding = JsonSerializer.Deserialize<float[]>(embeddingJson);

            if (embedding == null)
                throw new InvalidOperationException("Failed to deserialize embedding");

            var recordId = id ?? Guid.NewGuid().ToString();
            var payload = metadata ?? new Dictionary<string, object> { { "text", text } };

            // Use dynamic collection for upsert
            dynamic collection = _qdrantStore.GetDynamicCollection(_settings.CollectionName, null);

            var record = new Dictionary<string, object?>();
            record["id"] = Guid.Parse(recordId);
            record["payload"] = payload;
            record["vector"] = embedding;

            await collection.UpsertAsync(new[] { (object)record }, cancellationToken);

            _logger.LogInformation("Stored text embedding {Id}: '{Text}'", recordId, text[..Math.Min(50, text.Length)]);
            return $"Stored: {recordId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing text");
            throw;
        }
    }

    /// <summary>
    /// Search similar texts by generating embedding and querying Qdrant
    /// </summary>
    public async Task<List<QdrantSearchResult>> SearchAsync(
        string query,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryEmbeddingJson = await _embeddingService.CreateEmbeddingAsync(query, cancellationToken);
            var queryEmbedding = JsonSerializer.Deserialize<float[]>(queryEmbeddingJson);

            if (queryEmbedding == null)
                throw new InvalidOperationException("Failed to deserialize query embedding");

            // Use dynamic collection to search
            dynamic collection = _qdrantStore.GetDynamicCollection(_settings.CollectionName, null);

            var results = await collection.SearchAsync(queryEmbedding, topK, cancellationToken);

            _logger.LogInformation("Searched with query '{Query}', found {Count} results", query[..Math.Min(50, query.Length)], ((ICollection<object>)results).Count);

            return ((IEnumerable<object>)results).Select(r => {
                dynamic dr = r;
                string? idStr = dr.Id?.ToString() ?? dr.Point?.Id?.ToString() ?? dr.PointId?.ToString();
                Guid id = Guid.TryParse(idStr, out var g) ? g : Guid.Empty;
                float score = dr.Score;
                return new QdrantSearchResult { Id = id, Score = score, Text = null };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching");
            throw;
        }
    }

    /// <summary>
    /// Batch store multiple texts with embeddings
    /// </summary>
    public async Task<List<string>> StoreBatchAsync(
        List<(string Text, string? Id, Dictionary<string, object>? Metadata)> entries,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        try
        {
            foreach (var (text, id, metadata) in entries)
            {
                var result = await StoreTextAsync(text, id, metadata, cancellationToken);
                results.Add(result);
            }

            _logger.LogInformation("Batch stored {Count} text entries", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch store");
            throw;
        }
    }

    /// <summary>
    /// Get direct access to Qdrant client for advanced operations
    /// </summary>
    public global::Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore GetQdrantStore() => _qdrantStore;

    /// <summary>
    /// Get direct access to embedding service
    /// </summary>
    public IEmbeddingService GetEmbeddingService() => _embeddingService;
}

