using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using BoslaPlatform.Infrastructure.AI.OpenAi.Dtos;

namespace BoslaPlatform.Infrastructure.AI.OpenAi;

public class OpenAiChatService : IChatService
{
    private readonly OpenAISettings _settings;
    private readonly HttpClient _http;

    public OpenAiChatService(IOptions<OpenAISettings> options, IHttpClientFactory factory)
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

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return string.Empty;

        var payload = new ChatRequest { model = _settings.ChatModel, messages = new[] { new ChatMessage { role = "user", content = prompt } } };
        var resp = await _http.PostAsJsonAsync("/v1/chat/completions", payload, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
        if (doc?.choices?.Length > 0)
            return doc.choices[0].message.content;
        return string.Empty;
    }
}
