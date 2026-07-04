using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Utilities;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Infrastructure.STT.Providers.Agora.Configuration;
using BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Requests;
using BoslaPlatform.Infrastructure.STT.Providers.Agora.Models.Responses;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Services;

internal sealed class AgoraCloudSTTApiClient
{
    private readonly HttpClient _http;
    private readonly AgoraSettings _settings;
    private readonly ILogger<AgoraCloudSTTApiClient> _logger;

    public AgoraCloudSTTApiClient(
        HttpClient http,
        IOptions<AgoraSettings> options,
        ILogger<AgoraCloudSTTApiClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal async Task<Result<StartTaskResponse>> StartTaskAsync(
        string channelName,
        string languageCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            return Error.Validation("Agora.STT.MissingChannelName", "Channel name is required for STT.");
        if (string.IsNullOrWhiteSpace(languageCode))
            return Error.Validation("Agora.STT.MissingLanguage", "Language code is required for STT.");
        if (string.IsNullOrWhiteSpace(_settings.AppId))
            return Error.Failure("Agora.Configuration.AppIdMissing", "Agora AppId is not configured.");
        if (string.IsNullOrWhiteSpace(_settings.STTBaseUrl))
            return Error.Failure("Agora.Configuration.BaseUrlMissing", "Agora STTBaseUrl is not configured.");

        var url = BuildJoinEndpoint();

        var subBotUid = GenerateSTTUid("bosla-stt-sub").ToString();
        var pubBotUid = GenerateSTTUid("bosla-stt-pub").ToString();

        var requestBody = new StartTaskRequest
        {
            Name = $"bosla_stt_{channelName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Languages = [languageCode],
            MaxIdleTime = 60,
            RtcConfig = new RtcConfig
            {
                ChannelName = channelName,
                SubBotUid = subBotUid,
                PubBotUid = pubBotUid,
                SubscribeAudioUids = ["all"]
            }
        };

        _logger.LogInformation(
            "STT StartTask started for channel {ChannelName}, language {Language}",
            channelName, languageCode);

        var result = await SendAsync<StartTaskResponse>(HttpMethod.Post, url, requestBody, ct);

        if (result.IsError)
        {
            _logger.LogWarning("STT StartTask failed for channel {ChannelName}", channelName);
            return result.Errors;
        }

        var response = result.Value;

        if (string.IsNullOrWhiteSpace(response.AgentId))
        {
            _logger.LogError("STT StartTask response for channel {ChannelName} is missing agent_id", channelName);
            return Error.Unexpected("Agora.STT.MissingAgentId", "STT StartTask response did not contain an agent_id.");
        }

        _logger.LogInformation(
            "STT StartTask completed for channel {ChannelName}, agentId={AgentId}",
            channelName, response.AgentId);

        return Result<StartTaskResponse>.Success(response);
    }

    private string BuildJoinEndpoint() =>
        AgoraSTTEndpoints.BuildJoinEndpoint(_settings.STTBaseUrl, _settings.AppId);

    private async Task<Result<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body = null,
        CancellationToken ct = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, RecordingJsonDefaults.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Agora STT API request to {Url} failed after {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
            return Error.Unexpected("Agora.STT.RequestFailed", ex.Message);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Received {StatusCode} for {Method} {Url} in {ElapsedMs}ms",
            (int)response.StatusCode, method, url, stopwatch.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Agora STT API returned {StatusCode} for {Method} {Url}: {ErrorBody}",
                (int)response.StatusCode, method, url, errorBody);

            return MapError<T>((int)response.StatusCode, errorBody);
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var deserialized = JsonSerializer.Deserialize<T>(content, RecordingJsonDefaults.Options);

        if (deserialized is null)
            return Error.Unexpected("Agora.STT.InvalidResponse", "Empty or invalid response from Agora STT API.");

        return Result<T>.Success(deserialized);
    }

    private static Result<T> MapError<T>(int statusCode, string errorBody) where T : class
    {
        Error error = statusCode switch
        {
            400 => Error.Validation("Agora.STT.BadRequest", $"Agora STT API returned 400: {TruncateErrorBody(errorBody)}"),
            401 => Error.Unauthorized("Agora.STT.Unauthorized", "Agora STT API returned 401. Check CustomerId and CustomerSecret configuration."),
            403 => Error.Forbidden("Agora.STT.Forbidden", "Agora STT API returned 403. Verify Agora account permissions."),
            404 => Error.NotFound("Agora.STT.NotFound", "Agora STT API returned 404. Verify AppId and endpoint URL."),
            409 => Error.Conflict("Agora.STT.Conflict", $"Agora STT API returned 409: {TruncateErrorBody(errorBody)}"),
            429 => Error.Failure("Agora.STT.RateLimited", "Agora STT API returned 429. Rate limit exceeded. Retry later."),
            >= 500 and < 600 => Error.Unexpected("Agora.STT.ServerError", $"Agora STT API returned {statusCode}. Server error."),
            _ => Error.Failure("Agora.STT.ApiError", $"Agora STT API returned status {statusCode}.")
        };
        return Result<T>.Failure(error);
    }

    private static string TruncateErrorBody(string errorBody, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(errorBody))
            return string.Empty;

        return errorBody.Length <= maxLength
            ? errorBody
            : errorBody[..maxLength] + "...";
    }

    private static uint GenerateSTTUid(string prefix)
    {
        return (uint)(HashCode.Combine(prefix, Guid.NewGuid()) & 0x7FFFFFFF);
    }
}
