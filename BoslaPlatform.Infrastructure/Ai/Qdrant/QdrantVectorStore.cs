using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoslaPlatform.Infrastructure.AI.Qdrant;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _qdrant;
    private readonly IAppDbContext _db;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantVectorStore> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public QdrantVectorStore(QdrantClient qdrant, IAppDbContext db, IOptions<QdrantSettings> opts, ILogger<QdrantVectorStore> logger)
    {
        _qdrant = qdrant;
        _db = db;
        _settings = opts.Value;
        _logger = logger;
    }

    public async Task StoreEmbeddingAsync(Guid specialistId, string embeddingVector, string model, string contentHash, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(embeddingVector))
        {
            _logger.LogError("Embedding vector is null or empty for specialist {SpecialistId}", specialistId);
            throw new InvalidOperationException("Embedding vector cannot be null or empty");
        }

        // store in DB
        // Note: The unique index on SpecialistId means only one embedding per specialist
        var existing = await _db.Set<SpecialistEmbedding>().FirstOrDefaultAsync(se => se.SpecialistId == specialistId, cancellationToken);
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
            existing.ContentHash = contentHash;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // upsert into Qdrant
        var vec = DeserializeEmbeddingVector(embeddingVector, specialistId);
        if (vec.Length == 0)
        {
            _logger.LogError("Failed to deserialize embedding vector for specialist {SpecialistId}. Vector length is 0. JSON: {Json}", specialistId, embeddingVector);
            throw new InvalidOperationException($"Failed to deserialize embedding vector for specialist {specialistId}");
        }

        await _qdrant.UpsertPointAsync(specialistId, vec, new { specialistId = specialistId.ToString(), contentHash }, cancellationToken);
    }

    public async Task<IList<(Guid SpecialistId, float Score)>> SearchSimilarAsync(string queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        var vec = DeserializeEmbeddingVector(queryEmbedding, null);
        var results = await _qdrant.SearchAsync(vec, topK, cancellationToken);
        return results.Select(r => (r.Id, r.Score)).ToList();
    }

    private float[] DeserializeEmbeddingVector(string embeddingJson, Guid? specialistId = null)
    {
        try
        {
            // Try to deserialize as float array
            var result = JsonSerializer.Deserialize<float[]>(embeddingJson, JsonOptions);
            if (result is not null && result.Length > 0)
            {
                _logger.LogDebug("Successfully deserialized embedding vector as float[] with length {Length}", result.Length);
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to deserialize as float[]");
            // If standard deserialization fails, try alternative approaches
            try
            {
                // Try deserializing as double array and converting to float
                var doubleArray = JsonSerializer.Deserialize<double[]>(embeddingJson, JsonOptions);
                if (doubleArray is not null && doubleArray.Length > 0)
                {
                    var floatArray = doubleArray.Select(d => (float)d).ToArray();
                    _logger.LogDebug("Successfully deserialized embedding vector as double[] and converted to float[] with length {Length}", floatArray.Length);
                    return floatArray;
                }
            }
            catch (JsonException ex2)
            {
                _logger.LogDebug(ex2, "Failed to deserialize as double[]");
            }

            try
            {
                // Try deserializing as a nested object with an array property
                using var doc = JsonDocument.Parse(embeddingJson);
                var root = doc.RootElement;

                // Check for common property names
                var propertyNames = new[] { "embedding", "values", "value", "vector", "data" };
                foreach (var propName in propertyNames)
                {
                    if (root.TryGetProperty(propName, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.Array)
                        {
                            var floats = new List<float>();
                            foreach (var element in prop.EnumerateArray())
                            {
                                if (element.TryGetSingle(out var f))
                                    floats.Add(f);
                                else if (element.TryGetDouble(out var d))
                                    floats.Add((float)d);
                            }
                            if (floats.Count > 0)
                            {
                                _logger.LogDebug("Successfully extracted embedding from nested property '{Property}' with length {Length}", propName, floats.Count);
                                return floats.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception ex3)
            {
                _logger.LogDebug(ex3, "Failed to extract from nested properties");
            }
        }

        // Log the JSON for debugging if specialist ID is provided
        if (specialistId.HasValue)
        {
            _logger.LogWarning("Could not deserialize embedding vector for specialist {SpecialistId}. Raw JSON (first 500 chars): {Json}", specialistId, embeddingJson[..Math.Min(500, embeddingJson.Length)]);
        }

        // Fallback to empty array (will be caught by caller if needed)
        return Array.Empty<float>();
    }
}
