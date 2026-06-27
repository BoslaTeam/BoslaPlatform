using BoslaPlatform.Infrastructure.AI.Qdrant.Dtos;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Qdrant;

public class QdrantClient
{
    private readonly QdrantSettings _settings;
    private readonly HttpClient _http;
    private readonly ILogger<QdrantClient> _logger;

    public QdrantClient(IOptions<QdrantSettings> opts, HttpClient http, ILogger<QdrantClient> logger)
    {
        _settings = opts.Value;
        _http = http;
        _logger = logger;
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _http.BaseAddress = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrEmpty(_settings.ApiKey))
            _http.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        // Ensure the collection is created with the configured vector size to avoid dimension mismatches.
        var payload = new CreateCollectionRequest { name = _settings.CollectionName, vectors = new { size = _settings.VectorSize, distance = "Cosine" } };
        _logger.LogInformation("Ensuring Qdrant collection '{Collection}' with vector size {Size}", _settings.CollectionName, _settings.VectorSize);
        await _http.PutAsJsonAsync($"/collections/{_settings.CollectionName}", payload, cancellationToken);
    }

    public async Task UpsertPointAsync(Guid id, float[] vector, object payload, CancellationToken cancellationToken = default)
    {
        if (vector.Length != _settings.VectorSize)
        {
            _logger.LogError("Attempt to upsert vector with length {Len} but collection expects {Expected}", vector.Length, _settings.VectorSize);
            throw new InvalidOperationException($"Vector length {vector.Length} does not match configured collection vector size {_settings.VectorSize}.");
        }

        var req = new UpsertRequest
        {
            points = new[] { new UpsertPoint { id = id.ToString(), vector = vector, payload = payload } }
        };

        await _http.PostAsJsonAsync($"/collections/{_settings.CollectionName}/points?wait=true", req, cancellationToken);
    }

    public async Task<IList<(Guid Id, float Score)>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        if (queryVector.Length != _settings.VectorSize)
        {
            _logger.LogError("Search vector length {Len} does not match configured collection vector size {Expected}", queryVector.Length, _settings.VectorSize);
            throw new InvalidOperationException($"Query vector length {queryVector.Length} does not match configured collection vector size {_settings.VectorSize}.");
        }

        var req = new SearchRequest { vector = queryVector, limit = topK };
        string reqJson = JsonSerializer.Serialize(req);
        _logger.LogDebug("Qdrant search POST /collections/{Collection}/points/search payload: {Payload}", _settings.CollectionName, reqJson);

        var resp = await _http.PostAsJsonAsync($"/collections/{_settings.CollectionName}/points/search", req, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Qdrant search returned {Status}: {Body}", resp.StatusCode, body);
            resp.EnsureSuccessStatusCode();
        }

        var doc = await resp.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken);
        var list = new List<(Guid, float)>();
        if (doc?.result != null)
        {
            foreach (var p in doc.result)
            {
                if (Guid.TryParse(p.id, out var g))
                    list.Add((g, p.score));
                else
                    _logger.LogWarning("Qdrant returned non-guid id: {Id}", p.id);
            }
        }
        return list;
    }
}
