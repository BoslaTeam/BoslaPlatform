using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.VectorStore;

public class EfCoreVectorStore : IVectorStore
{
    private readonly IAppDbContext _db;

    public EfCoreVectorStore(IAppDbContext db)
    {
        _db = db;
    }

    public async Task StoreEmbeddingAsync(Guid specialistId, string embeddingVector, string model, string contentHash, CancellationToken cancellationToken = default)
    {
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
    }

    public async Task<IList<(Guid SpecialistId, float Score)>> SearchSimilarAsync(string queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        // Attempt to parse the incoming query embedding (expected JSON array) into float[]
        float[]? queryVec = TryParseVector(queryEmbedding);
        if (queryVec == null || queryVec.Length == 0)
            return Array.Empty<(Guid, float)>();

        var embeddings = await _db.Set<SpecialistEmbedding>().AsNoTracking().ToListAsync(cancellationToken);
        var results = new List<(Guid, float)>();
        var qNorm = Norm(queryVec);

        foreach (var se in embeddings)
        {
            var vec = TryParseVector(se.EmbeddingVector);
            if (vec == null || vec.Length == 0 || vec.Length != queryVec.Length)
                continue;

            var dot = Dot(queryVec, vec);
            var denom = qNorm * Norm(vec);
            var score = denom == 0 ? 0f : dot / denom;
            results.Add((se.SpecialistId, score));
        }

        return results.OrderByDescending(r => r.Item2).Take(topK).ToList();
    }

    private static float[]? TryParseVector(string vectorJson)
    {
        if (string.IsNullOrWhiteSpace(vectorJson))
            return null;

        try
        {
            // Expecting a JSON array like [0.1, 0.2, ...]
            return JsonSerializer.Deserialize<float[]>(vectorJson);
        }
        catch
        {
            // Try to parse comma-separated values as a fallback
            try
            {
                var parts = vectorJson.Trim('[', ']', ' ').Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Select(p => float.Parse(p)).ToArray();
            }
            catch
            {
                return null;
            }
        }
    }

    private static float Dot(float[] a, float[] b)
    {
        float sum = 0f;
        for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
        return sum;
    }

    private static float Norm(float[] a)
    {
        double s = 0.0;
        for (int i = 0; i < a.Length; i++) s += a[i] * a[i];
        return (float)Math.Sqrt(s);
    }
}
