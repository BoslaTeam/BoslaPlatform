using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Qdrant;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrant;
    private readonly IAppDbContext _db;
    private readonly QdrantSettings _settings;

    public QdrantVectorStore(QdrantClient qdrant, IAppDbContext db, IOptions<QdrantSettings> opts)
    {
        _qdrant = qdrant;
        _db = db;
        _settings = opts.Value;
    }

    public async Task StoreEmbeddingAsync(Guid specialistId, string embeddingVector, string model, string contentHash, CancellationToken cancellationToken = default)
    {
        // store in DB
        var existing = await _db.Set<SpecialistEmbedding>().FirstOrDefaultAsync(se => se.SpecialistId == specialistId && se.ContentHash == contentHash, cancellationToken);
        if (existing is null)
        {
            var se = new SpecialistEmbedding
            {
                SpecialistId = specialistId,
                EmbeddingVector = embeddingVector,
                EmbeddingModel = model,
                ContentHash = contentHash
            };
            _db.Set<SpecialistEmbedding>().Add(se);
        }
        else
        {
            existing.EmbeddingVector = embeddingVector;
            existing.EmbeddingModel = model;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // upsert into Qdrant
        var vec = JsonSerializer.Deserialize<float[]>(embeddingVector) ?? Array.Empty<float>();
        await _qdrant.UpsertPointAsync(specialistId, vec, new { specialistId = specialistId.ToString(), contentHash }, cancellationToken);
    }

    public async Task<IList<(Guid SpecialistId, float Score)>> SearchSimilarAsync(string queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        var vec = JsonSerializer.Deserialize<float[]>(queryEmbedding) ?? Array.Empty<float>();
        var results = await _qdrant.SearchAsync(vec, topK, cancellationToken);
        return results.Select(r => (r.Id, r.Score)).ToList();
    }
}
