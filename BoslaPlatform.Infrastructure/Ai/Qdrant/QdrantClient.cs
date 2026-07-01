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
        {
            // Support both Bearer token format (Qdrant Cloud) and api-key header format (local Qdrant)
            // Try Bearer token first (Qdrant Cloud standard)
            if (_settings.ApiKey.StartsWith("ey"))  // JWT tokens start with 'ey'
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
                _logger.LogInformation("Configured Qdrant client with Bearer token authorization (JWT detected)");
            }
            else
            {
                _http.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
                _logger.LogInformation("Configured Qdrant client with api-key header authorization");
            }
        }
        _logger.LogInformation("Qdrant client initialized with BaseUrl: {Url}, Collection: {Collection}, VectorSize: {Size}",
            _settings.BaseUrl ?? "not set", _settings.CollectionName, _settings.VectorSize);
    }

    /// <summary>
    /// Retry helper for transient failures (timeouts, 503, 429, etc.)
    /// </summary>
    private async Task<HttpResponseMessage> RetryablePostAsync(string endpoint, HttpContent content, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                var resp = await _http.PostAsync(endpoint, content, cancellationToken);

                // Retry on transient errors
                if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                    resp.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                    resp.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    (int)resp.StatusCode == 429) // Too many requests
                {
                    if (attempt < maxRetries)
                    {
                        var delay = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 10000); // Exponential backoff, max 10s
                        _logger.LogWarning("Qdrant {Endpoint} transient error {Status}, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", 
                            endpoint, resp.StatusCode, delay, attempt, maxRetries);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }

                return resp;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                if (attempt < maxRetries)
                {
                    var delay = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 10000);
                    _logger.LogWarning(ex, "Qdrant {Endpoint} timeout, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", 
                        endpoint, delay, attempt, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to complete POST to {endpoint} after {maxRetries} retries");
    }

    /// <summary>
    /// Retry helper for PUT requests (for upserts)
    /// </summary>
    private async Task<HttpResponseMessage> RetryablePutAsync(string endpoint, HttpContent content, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                var resp = await _http.PutAsync(endpoint, content, cancellationToken);

                // Retry on transient errors
                if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                    resp.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                    resp.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    (int)resp.StatusCode == 429) // Too many requests
                {
                    if (attempt < maxRetries)
                    {
                        var delay = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 10000); // Exponential backoff, max 10s
                        _logger.LogWarning("Qdrant {Endpoint} (PUT) transient error {Status}, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", 
                            endpoint, resp.StatusCode, delay, attempt, maxRetries);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }

                return resp;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                if (attempt < maxRetries)
                {
                    var delay = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 10000);
                    _logger.LogWarning(ex, "Qdrant {Endpoint} (PUT) timeout, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", 
                        endpoint, delay, attempt, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to complete PUT to {endpoint} after {maxRetries} retries");
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        // Ensure the collection is created with the configured vector size to avoid dimension mismatches.
        var payload = new CreateCollectionRequest { name = _settings.CollectionName, vectors = new { size = _settings.VectorSize, distance = "Cosine" } };
        _logger.LogInformation("Ensuring Qdrant collection '{Collection}' with vector size {Size} at BaseAddress: {BaseAddress}", 
            _settings.CollectionName, _settings.VectorSize, _http.BaseAddress);
        try
        {
            var resp = await _http.PutAsJsonAsync($"/collections/{_settings.CollectionName}", payload, cancellationToken);
            var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Qdrant collection '{Collection}' ensured successfully", _settings.CollectionName);
            }
            else
            {
                _logger.LogWarning("⚠ EnsureCollection returned status {Status}. Response: {Body}", 
                    resp.StatusCode, respBody.Substring(0, Math.Min(500, respBody.Length)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection '{Collection}'", _settings.CollectionName);
            throw;
        }
    }

    public async Task UpsertPointAsync(Guid id, float[] vector, object payload, CancellationToken cancellationToken = default)
    {
        if (vector.Length != _settings.VectorSize)
        {
            _logger.LogError("Attempt to upsert vector with length {Len} but collection expects {Expected}", vector.Length, _settings.VectorSize);
            throw new InvalidOperationException($"Vector length {vector.Length} does not match configured collection vector size {_settings.VectorSize}.");
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Primary (new) points format
        var pointsBody = new
        {
            points = new[]
            {
                new {
                    id = id.ToString(),
                    vector = vector,
                    payload = payload
                }
            }
        };

        string reqJson = JsonSerializer.Serialize(pointsBody, jsonOptions);
        _logger.LogInformation("Qdrant upsert: id={Id}, vectorLen={Len}. Request payload (first 1000 chars): {Payload}", id, vector.Length, reqJson.Substring(0, Math.Min(1000, reqJson.Length)));

        using var content = new StringContent(reqJson, System.Text.Encoding.UTF8, "application/json");
        // Qdrant upsert uses PUT (not POST) to /collections/{collection_name}/points?wait=true
        var endpoint = $"/collections/{_settings.CollectionName}/points?wait=true";
        var fullUrl = new Uri(_http.BaseAddress ?? new Uri("http://localhost"), endpoint).ToString();
        _logger.LogInformation("Qdrant upsert: Full URL (PUT)={Url}", fullUrl);

        // Use retry logic for transient failures
        var resp = await RetryablePutAsync(endpoint, content, maxRetries: 3, cancellationToken);
        var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Qdrant upsert response status: {Status} ({StatusCode}). Response body (first 500 chars): {Body}", 
            resp.StatusCode, (int)resp.StatusCode, respBody.Substring(0, Math.Min(500, respBody.Length)));

        if (resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("Qdrant upsert succeeded with status {Status}", resp.StatusCode);
            var readBack = await GetPointAsync(id, cancellationToken);
            if (!string.IsNullOrEmpty(readBack))
            {
                _logger.LogInformation("✓ Readback point after upsert verified: {Point}", readBack.Length > 500 ? readBack.Substring(0, 500) + "..." : readBack);
            }
            else
            {
                _logger.LogWarning("⚠ Readback returned null for id {Id} — point may not have been persisted to Qdrant Cloud", id);
            }
            return;
        }

        _logger.LogError("Qdrant upsert failed with status {Status}. Response body (first 1000 chars): {Body}", 
            resp.StatusCode, respBody.Substring(0, Math.Min(1000, respBody.Length)));
        _logger.LogError("Response headers: {Headers}", string.Join(", ", resp.Headers.Select(h => $"{h.Key}={string.Join("|", h.Value)}")));

        // 404 suggests the endpoint path is wrong or collection doesn't exist
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError("Got 404 Not Found from Qdrant. Collection '{Collection}' may not exist or endpoint is incorrect. Endpoint was: PUT /collections/{Collection}/points?wait=true", 
                _settings.CollectionName);
        }

        // If server expects the legacy 'ids/vectors/payloads' format, try that as fallback
        if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest && !string.IsNullOrEmpty(respBody) && respBody.Contains("missing field `ids`"))
        {
            _logger.LogInformation("Qdrant returned 'missing ids' error; retrying using legacy ids/vectors/payloads format");
            var alt = new
            {
                ids = new[] { id.ToString() },
                vectors = new[] { vector },
                payloads = new[] { payload }
            };

            string altJson = JsonSerializer.Serialize(alt, jsonOptions);
            _logger.LogInformation("Qdrant upsert (fallback) request payload (first 1000 chars): {Payload}", altJson.Substring(0, Math.Min(1000, altJson.Length)));

            using var content2 = new StringContent(altJson, System.Text.Encoding.UTF8, "application/json");
            // Use retry logic for fallback as well (also use PUT)
            var resp2 = await RetryablePutAsync($"/collections/{_settings.CollectionName}/points?wait=true", content2, maxRetries: 3, cancellationToken);
            var respBody2 = await resp2.Content.ReadAsStringAsync(cancellationToken);

            if (resp2.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Qdrant upsert fallback succeeded with status {Status}", resp2.StatusCode);
                var readBack = await GetPointAsync(id, cancellationToken);
                if (!string.IsNullOrEmpty(readBack))
                {
                    _logger.LogInformation("✓ Readback point after fallback upsert verified: {Point}", readBack.Length > 500 ? readBack.Substring(0, 500) + "..." : readBack);
                }
                else
                {
                    _logger.LogWarning("⚠ Readback returned null after fallback for id {Id}", id);
                }
                return;
            }

            _logger.LogError("Qdrant upsert fallback failed with status {Status}. Response (first 1000 chars): {Body}", resp2.StatusCode, respBody2.Substring(0, Math.Min(1000, respBody2.Length)));
            _logger.LogError("Qdrant upsert fallback request body (first 2000 chars): {Payload}", altJson.Substring(0, Math.Min(2000, altJson.Length)));
            resp2.EnsureSuccessStatusCode();
        }

        _logger.LogError("Qdrant upsert request failed with status {Status}. Response (first 1000 chars): {Body}", resp.StatusCode, respBody.Substring(0, Math.Min(1000, respBody.Length)));
        _logger.LogError("Qdrant upsert request body (first 2000 chars): {Payload}", reqJson.Substring(0, Math.Min(2000, reqJson.Length)));
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetPointAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var resp = await _http.GetAsync($"/collections/{_settings.CollectionName}/points/{id}", cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                return body;
            }

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Point {Id} not found", id);
                return null;
            }

            _logger.LogError("GetPoint returned {Status}: {Body}", resp.StatusCode, body);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPoint failed for {Id}", id);
            return null;
        }
    }

    public async Task<string?> ScrollPointsAsync(int offset = 0, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new { offset = offset, limit = limit };
            var resp = await _http.PostAsJsonAsync($"/collections/{_settings.CollectionName}/points/scroll", req, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                return body;
            }

            _logger.LogError("ScrollPoints returned {Status}: {Body}", resp.StatusCode, body);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScrollPoints failed");
            return null;
        }
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
