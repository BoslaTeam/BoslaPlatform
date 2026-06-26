using BoslaPlatform.Infrastructure.AI.Qdrant;
using Microsoft.Extensions.Options;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using BoslaPlatform.Application.Interfaces.AI;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

/// <summary>
/// Result from Qdrant memory search
/// </summary>
public class QdrantSearchResult
{
    public Guid Id { get; set; }
    public float Score { get; set; }
    public string? Text { get; set; }
}

/// <summary>
/// Semantic Kernel memory service backed by Qdrant vector database
/// Integrates Gemini embeddings with Qdrant vector storage through SK
/// </summary>
public class QdrantMemoryService
{
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantMemoryService> _logger;

    public QdrantMemoryService(
        QdrantClient qdrantClient,
        IEmbeddingService embeddingService,
        IOptions<QdrantSettings> opts,
        ILogger<QdrantMemoryService> logger)
    {
        _qdrantClient = qdrantClient ?? throw new ArgumentNullException(nameof(qdrantClient));
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
            // Generate embedding using Gemini
            var embeddingJson = await _embeddingService.CreateEmbeddingAsync(text, cancellationToken);
            var embedding = JsonSerializer.Deserialize<float[]>(embeddingJson);

            if (embedding == null)
                throw new InvalidOperationException("Failed to deserialize embedding");

            // Store in Qdrant with metadata
            var recordId = id ?? Guid.NewGuid().ToString();
            var payload = metadata ?? new Dictionary<string, object> { { "text", text } };

            await _qdrantClient.UpsertPointAsync(Guid.Parse(recordId), embedding, payload, cancellationToken);

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
            // Generate embedding for query using Gemini
            var queryEmbeddingJson = await _embeddingService.CreateEmbeddingAsync(query, cancellationToken);
            var queryEmbedding = JsonSerializer.Deserialize<float[]>(queryEmbeddingJson);

            if (queryEmbedding == null)
                throw new InvalidOperationException("Failed to deserialize query embedding");

            // Search in Qdrant
            var results = await _qdrantClient.SearchAsync(queryEmbedding, topK, cancellationToken);

            _logger.LogInformation("Searched with query '{Query}', found {Count} results", 
                query[..Math.Min(50, query.Length)], results.Count);

            // Convert to DTO format
            return results.Select(r => new QdrantSearchResult 
            { 
                Id = r.Id, 
                Score = r.Score, 
                Text = null 
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
    public QdrantClient GetQdrantClient() => _qdrantClient;

    /// <summary>
    /// Get direct access to embedding service
    /// </summary>
    public IEmbeddingService GetEmbeddingService() => _embeddingService;
}

