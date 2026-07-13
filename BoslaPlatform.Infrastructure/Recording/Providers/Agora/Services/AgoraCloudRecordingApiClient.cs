using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Configuration;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Utilities;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Infrastructure.Storage.Configuration;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services
{
    internal sealed class AgoraCloudRecordingApiClient
    {
        private readonly HttpClient _http;
        private readonly AgoraSettings _settings;
        private readonly ILogger<AgoraCloudRecordingApiClient> _logger;

        public AgoraCloudRecordingApiClient(
            HttpClient http,
            IOptions<AgoraSettings> options,
            IOptions<StorageOptions> storageOptions,
            ILogger<AgoraCloudRecordingApiClient> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal async Task<Result<string>> AcquireAsync(string channelName, string uid, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Acquire.MissingChannelName", "Channel name is required for Acquire.");
            if (string.IsNullOrWhiteSpace(uid))
                return Error.Validation("Agora.Acquire.MissingUid", "Recording UID is required for Acquire.");
            if (string.IsNullOrWhiteSpace(_settings.AppId))
                return Error.Failure("Agora.Configuration.AppIdMissing", "Agora AppId is not configured. Set 'AgoraSettings:AppId' in configuration.");
            if (string.IsNullOrWhiteSpace(_settings.CloudRecordingBaseUrl))
                return Error.Failure("Agora.Configuration.BaseUrlMissing", "Agora CloudRecordingBaseUrl is not configured. Set 'AgoraSettings:CloudRecordingBaseUrl' in configuration.");

            var url = BuildAcquireEndpoint();

            var requestBody = new AcquireRequest
            {
                Cname = channelName,
                Uid = uid,
                ClientRequest = new AcquireClientRequest()
            };

            _logger.LogInformation("Acquire started for channel {ChannelName}", channelName);

            var result = await SendAsync(HttpMethod.Post, url, requestBody, ct);

            if (result.IsError)
            {
                _logger.LogWarning("Acquire failed for channel {ChannelName}", channelName);
                return result.Errors;
            }

            using var doc = result.Value;
            var root = doc.RootElement;

            if (!root.TryGetProperty("resourceId", out var resourceIdElement))
            {
                _logger.LogError("Acquire response for channel {ChannelName} is missing resourceId property", channelName);
                return Error.Unexpected("Agora.Acquire.MissingResourceId", "Acquire response did not contain a resourceId property.");
            }

            var resourceId = resourceIdElement.GetString();
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                _logger.LogError("Acquire response for channel {ChannelName} contains empty resourceId", channelName);
                return Error.Unexpected("Agora.Acquire.EmptyResourceId", "Acquire response contained an empty resourceId.");
            }

            _logger.LogInformation("Acquire completed for channel {ChannelName}", channelName);

            return Result<string>.Success(resourceId);
        }

        internal async Task<Result<string>> StartAsync(
            string resourceId,
            StartRecordingRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return Error.Validation("Agora.Start.MissingResourceId", "ResourceId is required for Start.");
            if (request is null)
                return Error.Validation("Agora.Start.MissingRequest", "Start request body is required.");

            var url = BuildStartEndpoint(resourceId);

            _logger.LogInformation("Start started for channel {ChannelName}", request.Cname);

            var result = await SendAsync(HttpMethod.Post, url, request, ct);

            if (result.IsError)
            {
                _logger.LogWarning("Start failed for channel {ChannelName}", request.Cname);
                return result.Errors;
            }

            using var doc = result.Value;
            var root = doc.RootElement;

            if (!root.TryGetProperty("sid", out var sidElement))
            {
                _logger.LogError("Start response for channel {ChannelName} is missing sid property", request.Cname);
                return Error.Unexpected("Agora.Start.MissingSid", "Start response did not contain a sid property.");
            }

            var sid = sidElement.GetString();
            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogError("Start response for channel {ChannelName} contains empty sid", request.Cname);
                return Error.Unexpected("Agora.Start.EmptySid", "Start response contained an empty sid.");
            }

            _logger.LogInformation("Start completed for channel {ChannelName}", request.Cname);

            return Result<string>.Success(sid);
        }

        internal async Task<Result<JsonDocument>> StopAsync(
            string resourceId,
            string sid,
            StopRecordingRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return Error.Validation("Agora.Stop.MissingResourceId", "ResourceId is required for Stop.");
            if (string.IsNullOrWhiteSpace(sid))
                return Error.Validation("Agora.Stop.MissingSid", "SID is required for Stop.");
            if (request is null)
                return Error.Validation("Agora.Stop.MissingRequest", "Stop request body is required.");

            var url = BuildStopEndpoint(resourceId, sid);

            _logger.LogInformation("Stop started for channel {ChannelName}", request.Cname);

            var result = await SendAsync(HttpMethod.Post, url, request, ct);

            if (result.IsError)
            {
                _logger.LogWarning("Stop failed for channel {ChannelName}", request.Cname);
                return result;
            }

            _logger.LogInformation("Stop completed for channel {ChannelName}", request.Cname);

            return result;
        }

        internal async Task<Result<JsonDocument>> QueryAsync(string resourceId, string sid, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return Error.Validation("Agora.Query.MissingResourceId", "ResourceId is required for Query.");
            if (string.IsNullOrWhiteSpace(sid))
                return Error.Validation("Agora.Query.MissingSid", "SID is required for Query.");

            var url = BuildQueryEndpoint(resourceId, sid);

            _logger.LogInformation("Query started for resourceId={ResourceId}, sid={Sid}", resourceId, sid);

            var result = await SendAsync(HttpMethod.Get, url, null, ct);

            if (result.IsError)
            {
                _logger.LogWarning("Query failed for resourceId={ResourceId}, sid={Sid}", resourceId, sid);
                return result;
            }

            _logger.LogInformation("Query completed for resourceId={ResourceId}, sid={Sid}", resourceId, sid);

            return result;
        }

        internal async Task ReleaseAsync(string resourceId, string channelName, string uid, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                _logger.LogWarning("Release skipped: empty resourceId.");
                return;
            }

            var url = BuildReleaseEndpoint(resourceId);

            var requestBody = new ReleaseRequest
            {
                Cname = channelName,
                Uid = uid,
                ClientRequest = new ReleaseClientRequest()
            };

            _logger.LogInformation("Releasing Agora resource {ResourceId} for channel {ChannelName}", resourceId, channelName);

            var result = await SendAsync(HttpMethod.Post, url, requestBody, ct);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "Release failed for resource {ResourceId} (channel {ChannelName}). This is compensatory — the resource may be orphaned. Error: {Error}",
                    resourceId, channelName, result.Errors[0].Description);
                return;
            }

            _logger.LogInformation("Release completed for resource {ResourceId} (channel {ChannelName})", resourceId, channelName);
        }

        private string BuildAcquireEndpoint() =>
            AgoraCloudRecordingEndpoints.BuildAcquireEndpoint(_settings.CloudRecordingBaseUrl, _settings.AppId);

        private string BuildStartEndpoint(string resourceId) =>
            AgoraCloudRecordingEndpoints.BuildStartEndpoint(_settings.CloudRecordingBaseUrl, _settings.AppId, resourceId, _settings.RecordingMode.ToApiValue());

        private string BuildStopEndpoint(string resourceId, string sid) =>
            AgoraCloudRecordingEndpoints.BuildStopEndpoint(_settings.CloudRecordingBaseUrl, _settings.AppId, resourceId, sid, _settings.RecordingMode.ToApiValue());

        private string BuildQueryEndpoint(string resourceId, string sid) =>
            AgoraCloudRecordingEndpoints.BuildQueryEndpoint(_settings.CloudRecordingBaseUrl, _settings.AppId, resourceId, sid, _settings.RecordingMode.ToApiValue());

        private string BuildReleaseEndpoint(string resourceId) =>
            AgoraCloudRecordingEndpoints.BuildReleaseEndpoint(_settings.CloudRecordingBaseUrl, _settings.AppId, resourceId);

        private async Task<Result<JsonDocument>> SendAsync(
            HttpMethod method,
            string url,
            object? body = null,
            CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            using var request = new HttpRequestMessage(method, url);

            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body, RecordingJsonDefaults.Options);
                _logger.LogInformation("Agora Request Body:\n{Body}",json);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            _logger.LogInformation("Sending {Method} {Url}", method, url);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Agora API request to {Url} failed after {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return Error.Unexpected("Agora.RequestFailed", ex.Message);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Received {StatusCode} for {Method} {Url} in {ElapsedMs}ms",
                (int)response.StatusCode, method, url, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Agora API returned {StatusCode} for {Method} {Url}: {ErrorBody}",
                    (int)response.StatusCode, method, url, errorBody);

                return MapError((int)response.StatusCode, errorBody);
            }

            var contentStream = await response.Content.ReadAsStreamAsync(ct);
            var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);

            return Result<JsonDocument>.Success(document);
        }

        private static Error MapError(int statusCode, string errorBody)
        {
            return statusCode switch
            {
                400 => Error.Validation("Agora.BadRequest", $"Agora API returned 400: {TruncateErrorBody(errorBody)}"),
                401 => Error.Unauthorized("Agora.Unauthorized", "Agora API returned 401. Check CustomerId and CustomerSecret configuration."),
                403 => Error.Forbidden("Agora.Forbidden", "Agora API returned 403. Verify Agora account permissions."),
                404 => Error.NotFound("Agora.NotFound", "Agora API returned 404. Verify AppId and endpoint URL."),
                409 => Error.Conflict("Agora.Conflict", $"Agora API returned 409: {TruncateErrorBody(errorBody)}"),
                429 => Error.Failure("Agora.RateLimited", "Agora API returned 429. Rate limit exceeded. Retry later."),
                >= 500 and < 600 => Error.Unexpected("Agora.ServerError", $"Agora API returned {statusCode}. Server error."),
                _ => Error.Failure("Agora.ApiError", $"Agora API returned status {statusCode}.")
            };
        }

        private static string TruncateErrorBody(string errorBody, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(errorBody))
                return string.Empty;

            return errorBody.Length <= maxLength
                ? errorBody
                : errorBody[..maxLength] + "...";
        }
    }
}
