using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly GeminiSettings _settings;
    private readonly HttpClient _http;

    public GeminiEmbeddingService(IOptions<GeminiSettings> options, IHttpClientFactory factory)
    {
        _settings = options.Value;
        _http = factory.CreateClient("gemini");
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _http.BaseAddress = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            // Gemini/Generative Language expects x-goog-api-key header for API key auth
            if (!_http.DefaultRequestHeaders.Contains("x-goog-api-key"))
                _http.DefaultRequestHeaders.Add("x-goog-api-key", _settings.ApiKey);
        }
    }

        public async Task<string> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return "[]";

        // Gemini embedContent expects {model: "models/...", input: "..."} or POST to models/{model}:embedContent
        var payload = new { input = input };
        var endpoint = _settings.EmbeddingEndpoint.Replace("{model}", Uri.EscapeDataString(_settings.EmbeddingModel));

        const int maxAttempts = 3;
        var attempt = 0;
        var jitter = new Random();

        while (true)
        {
            attempt++;
            using var resp = await _http.PostAsJsonAsync(endpoint, payload, cancellationToken);

            if (resp.IsSuccessStatusCode)
            {
                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                // Exact Gemini embedContent response shape: { embedding: [float,...] }
                if (doc.RootElement.TryGetProperty("embedding", out var embTop) && embTop.ValueKind == JsonValueKind.Array)
                {
                    var floats = embTop.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                    return JsonSerializer.Serialize(floats);
                }

                // Vertex-like results: { results: [{ embedding: [...] }] }
                if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var r0 = results[0];
                    if (r0.TryGetProperty("embedding", out var emb2) && emb2.ValueKind == JsonValueKind.Array)
                    {
                        var floats = emb2.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                        return JsonSerializer.Serialize(floats);
                    }
                }

                // OpenAI-like fallback: { data: [{ embedding: [...] }] }
                if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var first = data[0];
                    if (first.TryGetProperty("embedding", out var embElem) && embElem.ValueKind == JsonValueKind.Array)
                    {
                        var floats = embElem.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                        return JsonSerializer.Serialize(floats);
                    }
                }

                return "[]";
            }

            if (resp.StatusCode != System.Net.HttpStatusCode.TooManyRequests || attempt >= maxAttempts)
            {
                resp.EnsureSuccessStatusCode();
            }

            TimeSpan delay;
            if (resp.Headers.RetryAfter?.Delta.HasValue == true)
            {
                delay = resp.Headers.RetryAfter.Delta.Value;
            }
            else if (resp.Headers.RetryAfter?.Date.HasValue == true)
            {
                delay = resp.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
            }
            else
            {
                var baseDelayMs = 500 * (1 << (attempt - 1));
                delay = TimeSpan.FromMilliseconds(baseDelayMs + jitter.Next(0, 200));
            }

            await Task.Delay(delay, cancellationToken);
        }
    }
}
