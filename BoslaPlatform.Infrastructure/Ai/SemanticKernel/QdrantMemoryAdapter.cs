using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.AI.Qdrant;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.SemanticKernel;

// Minimal adapter to expose QdrantVectorStore-like functions to Semantic Kernel style memory
public class QdrantMemoryAdapter
{
    private readonly QdrantClient _qdrant;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantMemoryAdapter> _logger;

    public QdrantMemoryAdapter(QdrantClient qdrant, IOptions<QdrantSettings> opts, ILogger<QdrantMemoryAdapter> logger)
    {
        _qdrant = qdrant;
        _settings = opts.Value;
        _logger = logger;
    }

    public async Task UpsertAsync(Guid id, float[] vector, object metadata, CancellationToken cancellationToken = default)
    {
        await _qdrant.EnsureCollectionAsync(cancellationToken);
        await _qdrant.UpsertPointAsync(id, vector, metadata, cancellationToken);
    }

    public async Task<IList<(Guid Id, float Score)>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        return await _qdrant.SearchAsync(queryVector, topK, cancellationToken);
    }
}
