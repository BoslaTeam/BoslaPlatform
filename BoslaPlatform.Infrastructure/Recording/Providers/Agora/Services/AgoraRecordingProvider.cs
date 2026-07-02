using BoslaPlatform.Application.Interfaces.Video;
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

            var doc = stopResult.Value;
            var fileUrl = string.Empty;
            var fileSize = 0L;
            var duration = 0;

            // Extract file details from Agora's serverResponse if present
            if (doc.RootElement.TryGetProperty("serverResponse", out var serverResponse) &&
                serverResponse.TryGetProperty("fileList", out var fileList) &&
                fileList.GetArrayLength() > 0)
            {
                var firstFile = fileList[0];
                fileUrl = firstFile.TryGetProperty("fileName", out var fn) ? fn.GetString() ?? string.Empty : string.Empty;
                fileSize = firstFile.TryGetProperty("fileSize", out var fs) ? fs.GetInt64() : 0L;
                // Duration is not available in stop response; left as 0
            }

            _logger.LogInformation(
                "Recording stopped for channel {ChannelName}, fileUrl={FileUrl}, size={Size}",
                channelName, fileUrl, fileSize);

            return Result<StopRecordingResult>.Success(
                new StopRecordingResult(fileUrl, duration, fileSize));
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
    }
}
