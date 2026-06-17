using BoslaPlatform.Infrastructure.AI.Qdrant.Dtos;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BoslaPlatform.Infrastructure.AI.Qdrant;

public class QdrantClient
{
    private readonly QdrantSettings _settings;
    private readonly HttpClient _http;

    public QdrantClient(IOptions<QdrantSettings> opts, HttpClient http)
    {
        _settings = opts.Value;
        _http = http;
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _http.BaseAddress = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrEmpty(_settings.ApiKey))
            _http.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        var payload = new CreateCollectionRequest { name = _settings.CollectionName };
        await _http.PutAsJsonAsync($"/collections/{_settings.CollectionName}", payload, cancellationToken);
    }

    public async Task UpsertPointAsync(Guid id, float[] vector, object payload, CancellationToken cancellationToken = default)
    {
        var req = new UpsertRequest
        {
            points = new[] { new UpsertPoint { id = id.ToString(), vector = vector, payload = payload } }
        };

        await _http.PostAsJsonAsync($"/collections/{_settings.CollectionName}/points?wait=true", req, cancellationToken);
    }

    public async Task<IList<(Guid Id, float Score)>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        var req = new { vector = queryVector, top = topK };
        var resp = await _http.PostAsJsonAsync($"/collections/{_settings.CollectionName}/points/search", req, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken);
        var list = new List<(Guid, float)>();
        if (doc?.result != null)
        {
            foreach (var p in doc.result)
            {
                if (Guid.TryParse(p.id, out var g))
                    list.Add((g, p.score));
            }
        }
        return list;
    }
}
