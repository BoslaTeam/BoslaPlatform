using System.Text.Json;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services
{
    internal sealed class AgoraRecordingProvider : IRecordingProvider
    {
        private readonly AgoraCloudRecordingApiClient _client;
        private readonly AgoraSettings _settings;
        private readonly ILogger<AgoraRecordingProvider> _logger;

        public AgoraRecordingProvider(
            AgoraCloudRecordingApiClient client,
            IOptions<AgoraSettings> options,
            ILogger<AgoraRecordingProvider> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "Agora";

        public async Task<Result<StartRecordingResult>> StartRecordingAsync(
            string channelName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Provider.MissingChannelName", "Channel name is required.");

            _logger.LogInformation("Recording start requested for channel {ChannelName}", channelName);

            var uid = GenerateRecordingUid();

            var acquireResult = await _client.AcquireAsync(channelName, uid.ToString(), ct);

            if (acquireResult.IsError)
            {
                _logger.LogWarning("Acquire failed for channel {ChannelName}, recording cannot start", channelName);
                return acquireResult.Errors;
            }

            var resourceId = acquireResult.Value;

            _logger.LogInformation("Acquire succeeded for channel {ChannelName}, resourceId={ResourceId}", channelName, resourceId);

            var startRequest = BuildStartRequest(channelName, uid.ToString());

            var startResult = await _client.StartAsync(resourceId, startRequest, ct);

            if (startResult.IsError)
            {
                _logger.LogWarning(
                    "Start failed for channel {ChannelName} after Acquire succeeded (resourceId={ResourceId}). " +
                    "Releasing Agora resource to avoid orphaned allocations.",
                    channelName, resourceId);

                await _client.ReleaseAsync(resourceId, channelName, uid.ToString(), ct);

                return startResult.Errors;
            }

            var sid = startResult.Value;

            _logger.LogInformation(
                "Recording started for channel {ChannelName}, resourceId={ResourceId}, sid={Sid}",
                channelName, resourceId, sid);

            return Result<StartRecordingResult>.Success(
                new StartRecordingResult(resourceId, sid));
        }

        public async Task<Result<StopRecordingResult>> StopRecordingAsync(
            string channelName,
            string providerRecordingId,
            string? providerMetadata = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Provider.MissingChannelName", "Channel name is required.");
            if (string.IsNullOrWhiteSpace(providerRecordingId))
                return Error.Validation("Agora.Provider.MissingRecordingId", "Provider recording ID is required.");
            if (string.IsNullOrWhiteSpace(providerMetadata))
                return Error.Validation("Agora.Provider.MissingSid", "Recording SID (providerMetadata) is required for Agora Stop.");

            _logger.LogInformation("Recording stop requested for channel {ChannelName}", channelName);

            var stopRequest = BuildStopRequest(channelName, providerMetadata);

            var stopResult = await _client.StopAsync(providerRecordingId, providerMetadata, stopRequest, ct);

            if (stopResult.IsError)
            {
                _logger.LogWarning("Provider stop failed for channel {ChannelName}", channelName);
                return stopResult.Errors;
            }

            using var doc = stopResult.Value;
            var root = doc.RootElement;

            var fileUrl = string.Empty;
            var fileSize = 0L;
            var duration = 0;

            if (root.TryGetProperty("serverResponse", out var sr))
            {
                var files = ParseFileList(sr);
                var fileCount = files?.Count ?? 0;

                if (fileCount > 0)
                {
                    var first = files![0];
                    fileUrl = first.FileName;
                    fileSize = first.FileSize;
                }

                var summary = files is not null
                    ? new RecordingSummary(fileCount, files.Sum(f => f.FileSize))
                    : null;

                _logger.LogInformation(
                    "Recording stopped for channel {ChannelName}, fileCount={FileCount}, totalSize={TotalSize}",
                    channelName, fileCount, summary?.TotalSizeBytes ?? 0);

                return Result<StopRecordingResult>.Success(
                    new StopRecordingResult(fileUrl, duration, fileSize, files, summary));
            }

            _logger.LogInformation(
                "Recording stopped for channel {ChannelName}, no serverResponse in payload",
                channelName);

            return Result<StopRecordingResult>.Success(
                new StopRecordingResult(fileUrl, duration, fileSize));
        }

        public async Task<Result<AcquireResult>> AcquireAsync(
            string channelName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Provider.MissingChannelName", "Channel name is required.");

            _logger.LogInformation("AcquireAsync called for channel {ChannelName}", channelName);

            var uid = GenerateRecordingUid().ToString();
            var result = await _client.AcquireAsync(channelName, uid, ct);

            if (result.IsError)
                return result.Errors;

            _logger.LogInformation("AcquireAsync completed for channel {ChannelName}", channelName);

            return new AcquireResult(result.Value);
        }

        public async Task<Result<QueryResult>> QueryAsync(
            string providerRecordingId,
            string sid,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(providerRecordingId))
                return Error.Validation("Agora.Provider.MissingRecordingId", "Provider recording ID is required.");
            if (string.IsNullOrWhiteSpace(sid))
                return Error.Validation("Agora.Provider.MissingSid", "SID is required for Query.");

            _logger.LogInformation("QueryAsync called for resourceId={ResourceId}, sid={Sid}",
                providerRecordingId, sid);

            var result = await _client.QueryAsync(providerRecordingId, sid, ct);

            if (result.IsError)
                return result.Errors;

            using var doc = result.Value;
            var root = doc.RootElement;

            var status = RecordingStatus.Processing;
            IReadOnlyList<RecordingFileInfo>? files = null;
            RecordingSummary? summary = null;

            if (root.TryGetProperty("serverResponse", out var sr))
            {
                var statusStr = sr.TryGetProperty("status", out var sts)
                    ? sts.GetString() ?? "unknown"
                    : "unknown";

                status = MapAgoraStatus(statusStr);
                files = ParseFileList(sr);

                if (files is not null)
                {
                    summary = new RecordingSummary(files.Count, files.Sum(f => f.FileSize));
                }
            }

            _logger.LogInformation(
                "QueryAsync completed for resourceId={ResourceId}, sid={Sid}, status={Status}, fileCount={FileCount}",
                providerRecordingId, sid, status, files?.Count ?? 0);

            return new QueryResult(status, providerRecordingId, sid, files, summary);
        }

        public Task<Result<RecordingStatusResult>> GetStatusAsync(
            string providerRecordingId,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        private StartRecordingRequest BuildStartRequest(string channelName, string uid)
        {
            var prefix = string.IsNullOrWhiteSpace(_settings.StorageFileNamePrefix)
                ? []
                : _settings.StorageFileNamePrefix.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            return new StartRecordingRequest
            {
                Cname = channelName,
                Uid = uid,
                ClientRequest = new StartClientRequest
                {
                    RecordingConfig = new RecordingConfig
                    {
                        MaxIdleTime = _settings.RecordingMaxIdleTime,
                        StreamTypes = _settings.RecordingStreamTypes,
                        ChannelType = 0,
                        VideoStreamType = 0,
                        SubscribeUidGroup = 0
                    },
                    RecordingFileConfig = new RecordingFileConfig
                    {
                        AvFileType = ["hls"]
                    },
                    StorageConfig = new StorageConfig
                    {
                        Vendor = _settings.StorageVendor,
                        Region = _settings.StorageRegion,
                        Bucket = _settings.StorageBucket,
                        AccessKey = _settings.StorageAccessKey,
                        SecretKey = _settings.StorageSecretKey,
                        FileNamePrefix = prefix
                    }
                }
            };
        }

        private static StopRecordingRequest BuildStopRequest(string channelName, string sid)
        {
            return new StopRecordingRequest
            {
                Cname = channelName,
                Uid = sid,
                ClientRequest = new StopClientRequest()
            };
        }

        private static uint GenerateRecordingUid()
        {
            return (uint)(HashCode.Combine("bosla-recording", Guid.NewGuid()) & 0x7FFFFFFF);
        }

        private static IReadOnlyList<RecordingFileInfo>? ParseFileList(JsonElement serverResponse)
        {
            if (!serverResponse.TryGetProperty("fileList", out var fileList) ||
                fileList.GetArrayLength() == 0)
            {
                return null;
            }

            var result = new List<RecordingFileInfo>(fileList.GetArrayLength());

            foreach (var file in fileList.EnumerateArray())
            {
                var fileName = file.TryGetProperty("fileName", out var fn)
                    ? fn.GetString() ?? string.Empty
                    : string.Empty;

                var fileSize = file.TryGetProperty("fileSize", out var fs)
                    ? fs.GetInt64()
                    : 0L;

                var sliceStartTime = file.TryGetProperty("sliceStartTime", out var sst)
                    ? sst.GetInt64()
                    : (long?)null;

                var mimeType = InferMimeType(fileName);

                result.Add(new RecordingFileInfo(
                    fileName,
                    fileName,
                    fileSize,
                    sliceStartTime.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(sliceStartTime.Value).UtcDateTime
                        : null,
                    mimeType));
            }

            return result;
        }

        private static string InferMimeType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var ext = System.IO.Path.GetExtension(fileName.AsSpan()).ToString();

            return ext.ToLowerInvariant() switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".mp4" => "video/mp4",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }

        private static RecordingStatus MapAgoraStatus(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "inprogress" or "processing" => RecordingStatus.Processing,
                "completed" or "stopped" => RecordingStatus.Completed,
                "failed" => RecordingStatus.Failed,
                "idle" or "notstarted" => RecordingStatus.Idle,
                "uploading" => RecordingStatus.Uploading,
                "uploaded" => RecordingStatus.Uploaded,
                "starting" => RecordingStatus.Starting,
                "cancelled" or "canceled" => RecordingStatus.Cancelled,
                _ => RecordingStatus.Processing
            };
        }
    }
}