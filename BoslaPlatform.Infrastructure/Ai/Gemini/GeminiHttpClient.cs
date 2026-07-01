using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BoslaPlatform.Infrastructure.Settings;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public class GeminiHttpClient
{
    private readonly HttpClient _http;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiHttpClient> _logger;

    public GeminiHttpClient(IOptions<GeminiSettings> opts, HttpClient http, ILogger<GeminiHttpClient> logger)
    {
        _settings = opts.Value;
        _http = http;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _http.BaseAddress = new Uri(_settings.BaseUrl);

        // default headers
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(_settings.ApiKey))
            _http.DefaultRequestHeaders.Add("x-goog-api-key", _settings.ApiKey);
    }

    public async Task<HttpResponseMessage> PostJsonAsync(string relativeOrAbsoluteUri, object payload, CancellationToken cancellationToken = default)
    {
        var requestUri = BuildUri(relativeOrAbsoluteUri);

        // Append API key as query parameter if configured
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            var separator = requestUri.Contains("?") ? "&" : "?";
            requestUri = $"{requestUri}{separator}key={Uri.EscapeDataString(_settings.ApiKey)}";
        }

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _logger.LogDebug("POST {Url} with payload: {Payload}", requestUri, json);
        var resp = await _http.PostAsync(requestUri, content, cancellationToken);
        _logger.LogDebug("POST response: {StatusCode}", resp.StatusCode);
        return resp;
    }

    private string BuildUri(string uri)
    {
        if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            return uri;
        return uri.StartsWith("/") ? uri : "/" + uri;
    }
}
