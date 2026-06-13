using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using BoslaPlatform.Infrastructure.AI.OpenAi.Dtos;

namespace BoslaPlatform.Infrastructure.AI.OpenAi;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly OpenAISettings _settings;
    private readonly HttpClient _http;

    public OpenAiEmbeddingService(IOptions<OpenAISettings> options, IHttpClientFactory factory)
    {
        _settings = options.Value;
        _http = factory.CreateClient("openai");
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _http.BaseAddress = new Uri(_settings.BaseUrl);
        }
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<string> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return "[]";

        var payload = new EmbeddingRequest { input = input, model = _settings.EmbeddingModel };
        var resp = await _http.PostAsJsonAsync("/v1/embeddings", payload, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        if (doc?.data?.Length > 0)
        {
            return System.Text.Json.JsonSerializer.Serialize(doc.data[0].embedding);
        }
        return "[]";
    }
}
