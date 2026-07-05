using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public class EmbeddingBatcher : IDisposable
{
    private readonly BoslaPlatform.Infrastructure.Settings.GeminiSettings _settings;
    private readonly GeminiHttpClient _client;
    private readonly ILogger<EmbeddingBatcher> _logger;
    private readonly SemaphoreSlim _semaphore;

    public EmbeddingBatcher(IOptions<BoslaPlatform.Infrastructure.Settings.GeminiSettings> opts, GeminiHttpClient client, ILogger<EmbeddingBatcher> logger)
    {
        _settings = opts.Value;
        _client = client;
        _logger = logger ?? NullLogger<EmbeddingBatcher>.Instance;
        _semaphore = new SemaphoreSlim(_settings.EmbeddingConcurrency);
    }

    public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Normalize model and build endpoint
            var model = _settings.EmbeddingModel ?? string.Empty;
            if (model.StartsWith("models/", System.StringComparison.OrdinalIgnoreCase))
                model = model.Substring("models/".Length);
            model = model.Trim('/');
            var endpoint = _settings.EmbeddingEndpoint.Replace("{model}", model);

            var payload = new
            {
                content = new
                {
                    parts = new[] { new { text = input } }
                }
            };

            using var resp = await _client.PostJsonAsync(endpoint, payload, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Gemini embedding response body: {Body}", body);

            var extracted = TryExtractEmbeddingFromJson(body);
            if (extracted != null) return extracted;

            try
            {
                var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
                var doc = await System.Text.Json.JsonSerializer.DeserializeAsync<GeminiEmbeddingResponse>(stream, cancellationToken: cancellationToken);
                if (doc?.Embedding != null) return doc.Embedding;
                if (doc?.Embedding_value != null) return doc.Embedding_value;
                if (doc?.Embeddings != null && doc.Embeddings.Length > 0 && doc.Embeddings[0].Embedding != null) return doc.Embeddings[0].Embedding;
                if (doc?.Data != null && doc.Data.Length > 0 && doc.Data[0].Embedding != null) return doc.Data[0].Embedding;
                if (doc?.Results != null && doc.Results.Length > 0 && doc.Results[0].Embedding != null) return doc.Results[0].Embedding;
            }
            catch (System.Text.Json.JsonException jex)
            {
                _logger.LogWarning(jex, "Failed to deserialize Gemini embedding DTOs");
            }

            _logger.LogError("Could not extract embedding from Gemini response");
            return Array.Empty<float>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static float[]? TryExtractEmbeddingFromJson(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("embedding", out var emb) && emb.ValueKind == System.Text.Json.JsonValueKind.Array)
                return ConvertNumberArray(emb);

            if (root.TryGetProperty("embedding", out emb) && emb.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var name in new[] { "values", "value", "vector" })
                {
                    if (emb.TryGetProperty(name, out var nested) && nested.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = ConvertNumberArray(nested);
                        if (arr != null) return arr;
                    }
                }
            }

            if (root.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var e) && e.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = ConvertNumberArray(e);
                        if (arr != null) return arr;
                    }
                    if (item.TryGetProperty("values", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = ConvertNumberArray(v);
                        if (arr != null) return arr;
                    }
                }
            }

            if (root.TryGetProperty("embeddings", out var embs) && embs.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in embs.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.Object && item.TryGetProperty("embedding", out var e) && e.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = ConvertNumberArray(e);
                        if (arr != null) return arr;
                    }
                }
            }

            if (root.TryGetProperty("results", out var results) && results.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var e) && e.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = ConvertNumberArray(e);
                        if (arr != null) return arr;
                    }
                }
            }

            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static float[]? ConvertNumberArray(System.Text.Json.JsonElement arr)
    {
        try
        {
            var list = new System.Collections.Generic.List<float>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    list.Add((float)el.GetDouble());
                }
                else if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (float.TryParse(el.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
                        list.Add(f);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            return list.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
