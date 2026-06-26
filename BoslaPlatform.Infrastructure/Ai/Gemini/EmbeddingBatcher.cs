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
            // Build the endpoint URL with the model name
            var endpoint = _settings.EmbeddingEndpoint.Replace("{model}", Uri.EscapeDataString(_settings.EmbeddingModel));

            // Google Gemini API requires the request in the format: { content: { parts: [{ text = "..." }] } }
            var payload = new
            {
                content = new
                {
                    parts = new[] { new { text = input } }
                }
            };

            //_logger.LogDebug("Embedding endpoint: {Endpoint}", endpoint);

            using var resp = await _client.PostJsonAsync(endpoint, payload, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            var doc = await System.Text.Json.JsonSerializer.DeserializeAsync<GeminiEmbeddingResponse>(stream, cancellationToken: cancellationToken);

            if (doc?.Embedding != null) return doc.Embedding;
            if (doc?.Embedding_value != null) return doc.Embedding_value;
            return Array.Empty<float>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
