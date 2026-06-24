using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public class GeminiChatService : IChatService
{
    private readonly GeminiSettings _settings;
    private readonly HttpClient _http;

    public GeminiChatService(IOptions<GeminiSettings> options, IHttpClientFactory factory)
    {
        _settings = options.Value;
        _http = factory.CreateClient("gemini");
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _http.BaseAddress = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            if (!_http.DefaultRequestHeaders.Contains("x-goog-api-key"))
                _http.DefaultRequestHeaders.Add("x-goog-api-key", _settings.ApiKey);
        }
    }

        public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return string.Empty;

        var payload = new { model = _settings.ChatModel, input = prompt };
        var resp = await _http.PostAsJsonAsync(_settings.ChatEndpoint, payload, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        // Exact Gemini Interactions shapes (preferred)
        if (doc.RootElement.TryGetProperty("output_text", out var outText) && outText.ValueKind == JsonValueKind.String)
            return outText.GetString() ?? string.Empty;
        if (doc.RootElement.TryGetProperty("outputText", out var outText2) && outText2.ValueKind == JsonValueKind.String)
            return outText2.GetString() ?? string.Empty;

        // Interactions: { output: [ { output_text: "...", content: [...], candidates: [...] } ] }
        if (doc.RootElement.TryGetProperty("output", out var output) && output.GetArrayLength() > 0)
        {
            var firstOut = output[0];
            if (firstOut.TryGetProperty("output_text", out var fo) && fo.ValueKind == JsonValueKind.String)
                return fo.GetString() ?? string.Empty;
            if (firstOut.TryGetProperty("outputText", out var fo2) && fo2.ValueKind == JsonValueKind.String)
                return fo2.GetString() ?? string.Empty;

            // candidates[].output_text
            if (firstOut.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var c0 = candidates[0];
                if (c0.ValueKind == JsonValueKind.Object)
                {
                    if (c0.TryGetProperty("output_text", out var cot) && cot.ValueKind == JsonValueKind.String)
                        return cot.GetString() ?? string.Empty;
                    if (c0.TryGetProperty("outputText", out var cot2) && cot2.ValueKind == JsonValueKind.String)
                        return cot2.GetString() ?? string.Empty;

                    // candidate.content[].text
                    if (c0.TryGetProperty("content", out var ccontent) && ccontent.ValueKind == JsonValueKind.Array && ccontent.GetArrayLength() > 0)
                    {
                        var cc0 = ccontent[0];
                        if (cc0.TryGetProperty("text", out var tcc) && tcc.ValueKind == JsonValueKind.String)
                            return tcc.GetString() ?? string.Empty;
                    }
                }
            }

            // output[].content[].text
            if (firstOut.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array && contentArr.GetArrayLength() > 0)
            {
                var item = contentArr[0];
                if (item.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString() ?? string.Empty;
            }
        }

        // Fallbacks: OpenAI-like and other shapes
        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;
            if (first.TryGetProperty("content", out var content2) && content2.ValueKind == JsonValueKind.String)
                return content2.GetString() ?? string.Empty;
        }

        if (doc.RootElement.TryGetProperty("candidates", out var candRoot) && candRoot.GetArrayLength() > 0)
        {
            var c0 = candRoot[0];
            if (c0.ValueKind == JsonValueKind.Object && c0.TryGetProperty("content", out var cc))
            {
                if (cc.ValueKind == JsonValueKind.String) return cc.GetString() ?? string.Empty;
                if (cc.ValueKind == JsonValueKind.Object && cc.TryGetProperty("text", out var txt)) return txt.GetString() ?? string.Empty;
            }
        }

        // Top-level fallbacks
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.NameEquals("text") || prop.NameEquals("outputText") || prop.NameEquals("output_text") || prop.NameEquals("response") || prop.NameEquals("output_text"))
            {
                if (prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
