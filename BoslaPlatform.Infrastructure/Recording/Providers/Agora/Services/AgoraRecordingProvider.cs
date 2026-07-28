using System.Diagnostics;
using System.Text.Json;
using AgoraIO.Media;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Models.Requests;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Utilities;
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
        private readonly IRecordingPipelineLog _pipelineLog;

        public AgoraRecordingProvider(
            AgoraCloudRecordingApiClient client,
            IOptions<AgoraSettings> options,
            ILogger<AgoraRecordingProvider> logger,
            IRecordingPipelineLog pipelineLog)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipelineLog = pipelineLog ?? throw new ArgumentNullException(nameof(pipelineLog));
        }

        /// <summary>
        /// Agora's wildcard for "every publisher currently in the channel".
        /// </summary>
        private const string AllStreams = "#allstream#";

        public string Name => "Agora";

        public async Task<Result<StartRecordingResult>> StartRecordingAsync(
            string channelName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Provider.MissingChannelName", "Channel name is required.");

            _logger.LogInformation("Recording start requested for channel {ChannelName}", channelName);

            var uid = GenerateRecordingUid();

            var recordingToken = BuildRecordingToken(channelName, uid);

            // RtcTokenBuilder2 returns an empty string (it does not throw) when the
            // App Certificate is malformed. Sending token:"" to a secured channel
            // lets the recorder join, capture nothing, and report success — refuse
            // to start rather than produce an empty recording.
            if (!string.IsNullOrWhiteSpace(_settings.AppCertificate) &&
                string.IsNullOrEmpty(recordingToken))
            {
                _logger.LogError(
                    "Recording token generation produced an empty token for channel {ChannelName}. " +
                    "Check that AgoraSettings:AppCertificate is a valid Agora App Certificate.",
                    channelName);

                _pipelineLog.Failed(
                    RecordingStage.Start,
                    RecordingLogContext.ForChannel(channelName) with { RecordingUid = uid.ToString() },
                    "Agora.Provider.TokenGenerationFailed",
                    "Recording RTC token generation returned an empty token.");

                return Error.Failure(
                    "Agora.Provider.TokenGenerationFailed",
                    "Failed to generate the recording RTC token. Verify the configured App Certificate.");
            }

            var logContext = RecordingLogContext.ForChannel(channelName) with
            {
                RecordingUid = uid.ToString()
            };

            _pipelineLog.Started(RecordingStage.Acquire, logContext);
            var acquireClock = Stopwatch.StartNew();

            var acquireResult = await _client.AcquireAsync(channelName, uid.ToString(), ct);
            acquireClock.Stop();

            if (acquireResult.IsError)
            {
                _pipelineLog.Failed(
                    RecordingStage.Acquire, logContext,
                    acquireResult.Errors[0].Code, acquireResult.Errors[0].Description,
                    duration: acquireClock.Elapsed);

                _logger.LogWarning("Acquire failed for channel {ChannelName}, recording cannot start", channelName);
                return acquireResult.Errors;
            }

            var resourceId = acquireResult.Value;
            logContext = logContext with { ResourceId = resourceId };

            _pipelineLog.Succeeded(RecordingStage.Acquire, logContext, acquireClock.Elapsed);

            _logger.LogInformation("Acquire succeeded for channel {ChannelName}, resourceId={ResourceId}", channelName, resourceId);

            var startRequest = BuildStartRequest(channelName, uid.ToString(), recordingToken);

            _pipelineLog.Started(RecordingStage.Start, logContext);
            var startClock = Stopwatch.StartNew();

            var startResult = await _client.StartAsync(resourceId, startRequest, ct);
            startClock.Stop();

            if (startResult.IsError)
            {
                _pipelineLog.Failed(
                    RecordingStage.Start, logContext,
                    startResult.Errors[0].Code, startResult.Errors[0].Description,
                    duration: startClock.Elapsed);

                _logger.LogWarning(
                    "Start failed for channel {ChannelName} after Acquire succeeded (resourceId={ResourceId}). " +
                    "Releasing Agora resource to avoid orphaned allocations.",
                    channelName, resourceId);

                await _client.ReleaseAsync(resourceId, channelName, uid.ToString(), ct);

                return startResult.Errors;
            }

            var sid = startResult.Value;
            logContext = logContext with { Sid = sid };

            _pipelineLog.Succeeded(RecordingStage.Start, logContext, startClock.Elapsed);

            _logger.LogInformation(
                "Recording started for channel {ChannelName}. RecordingUid={RecordingUid}, ResourceId={ResourceId}, SID={Sid}",
                channelName, uid.ToString(), resourceId, sid);

            return Result<StartRecordingResult>.Success(
                new StartRecordingResult(resourceId, sid, uid.ToString()));
        }

        public async Task<Result<StopRecordingResult>> StopRecordingAsync(
            string channelName,
            string providerRecordingId,
            string providerRecordingSid,
            string recordingUid,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                return Error.Validation("Agora.Provider.MissingChannelName", "Channel name is required.");
            if (string.IsNullOrWhiteSpace(providerRecordingId))
                return Error.Validation("Agora.Provider.MissingRecordingId", "Provider recording ID (ResourceId) is required.");
            if (string.IsNullOrWhiteSpace(providerRecordingSid))
                return Error.Validation("Agora.Provider.MissingSid", "Recording SID is required.");
            if (string.IsNullOrWhiteSpace(recordingUid))
                return Error.Validation("Agora.Provider.MissingUid", "Recording UID is required.");

            _logger.LogInformation(
                "Recording stop requested for channel {ChannelName}. RecordingUid={RecordingUid}, ResourceId={ResourceId}, SID={Sid}",
                channelName, recordingUid, providerRecordingId, providerRecordingSid);

            var stopRequest = BuildStopRequest(channelName, recordingUid);

            var stopContext = new RecordingLogContext
            {
                ChannelName = channelName,
                ResourceId = providerRecordingId,
                Sid = providerRecordingSid,
                RecordingUid = recordingUid
            };

            _pipelineLog.Started(RecordingStage.Stop, stopContext);
            var stopClock = Stopwatch.StartNew();

            var stopResult = await _client.StopAsync(providerRecordingId, providerRecordingSid, stopRequest, ct);
            stopClock.Stop();

            if (stopResult.IsError)
            {
                // Agora returns 404 when the recording resource no longer exists — the
                // recording was already stopped (idle timeout via RecordingMaxIdleTime,
                // the channel was destroyed, or a prior stop already ran). "Already
                // stopped" is the desired end state, so treat it as an idempotent
                // success instead of a hard 404 that would reach the specialist or
                // block session completion (recording stop is best-effort).
                if (stopResult.Errors[0].Code == "Agora.NotFound")
                {
                    _logger.LogInformation(
                        "Provider stop for channel {ChannelName} returned 404 — recording already stopped. " +
                        "Treating as idempotent success. resourceId={ResourceId} sid={Sid}",
                        channelName, providerRecordingId, providerRecordingSid);

                    _pipelineLog.Succeeded(
                        RecordingStage.Stop, stopContext, stopClock.Elapsed,
                        new Dictionary<string, object?>
                        {
                            ["outcome"] = "already-stopped (Agora 404)",
                            ["fileCount"] = 0
                        });

                    return Result<StopRecordingResult>.Success(
                        new StopRecordingResult(string.Empty, 0, 0));
                }

                _pipelineLog.Failed(
                    RecordingStage.Stop, stopContext,
                    stopResult.Errors[0].Code, stopResult.Errors[0].Description,
                    duration: stopClock.Elapsed);

                _logger.LogWarning(
                    "Provider stop failed for channel {ChannelName}. resourceId={ResourceId} sid={Sid} uid={Uid}: {Error}",
                    channelName, providerRecordingId, providerRecordingSid, recordingUid,
                    stopResult.Errors[0].Description);
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
                var uploadingStatus = ParseUploadingStatus(sr);

                if (fileCount > 0)
                {
                    var first = files![0];
                    fileUrl = first.FileName;
                    fileSize = first.FileSize;
                }

                var summary = files is not null
                    ? new RecordingSummary(fileCount, files.Sum(f => f.FileSize))
                    : null;

                _pipelineLog.Succeeded(
                    RecordingStage.Stop, stopContext, stopClock.Elapsed,
                    new Dictionary<string, object?>
                    {
                        ["fileCount"] = fileCount,
                        ["totalSizeBytes"] = summary?.TotalSizeBytes ?? 0,
                        ["firstFile"] = fileUrl,
                        ["uploadingStatus"] = uploadingStatus.ToString()
                    });

                if (fileCount == 0)
                {
                    // Agora accepted the stop but captured nothing. Logged as a stage
                    // failure because an empty recording is a pipeline failure even
                    // though the HTTP call succeeded.
                    _pipelineLog.Failed(
                        RecordingStage.Stop, stopContext,
                        "Agora.Stop.NoFilesProduced",
                        "Stop succeeded but Agora returned an empty fileList — nothing was captured.",
                        duration: stopClock.Elapsed);
                }

                _logger.LogInformation(
                    "Recording stopped for channel {ChannelName}, fileCount={FileCount}, totalSize={TotalSize}",
                    channelName, fileCount, summary?.TotalSizeBytes ?? 0);

                return Result<StopRecordingResult>.Success(
                    new StopRecordingResult(fileUrl, duration, fileSize, files, summary, uploadingStatus));
            }

            _pipelineLog.Failed(
                RecordingStage.Stop, stopContext,
                "Agora.Stop.NoServerResponse",
                "Stop returned 200 but the payload had no serverResponse — no files were produced.",
                duration: stopClock.Elapsed);

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
            var uploadingStatus = AgoraUploadingStatus.Unknown;

            if (root.TryGetProperty("serverResponse", out var sr))
            {
                // Agora's Query serverResponse.status is a numeric recording-worker
                // state code, not a string; read it defensively so a JSON number does
                // not throw. The upload signal we actually gate on is uploadingStatus.
                var statusStr = sr.TryGetProperty("status", out var sts)
                    ? (sts.ValueKind == JsonValueKind.String
                        ? sts.GetString() ?? "unknown"
                        : sts.ToString())
                    : "unknown";

                status = MapAgoraStatus(statusStr);
                files = ParseFileList(sr);
                uploadingStatus = ParseUploadingStatus(sr);

                if (files is not null)
                {
                    summary = new RecordingSummary(files.Count, files.Sum(f => f.FileSize));
                }
            }

            _logger.LogInformation(
                "QueryAsync completed for resourceId={ResourceId}, sid={Sid}, status={Status}, uploadingStatus={UploadingStatus}, fileCount={FileCount}",
                providerRecordingId, sid, status, uploadingStatus, files?.Count ?? 0);

            return new QueryResult(status, providerRecordingId, sid, files, summary, uploadingStatus);
        }

        public Task<Result<RecordingStatusResult>> GetStatusAsync(
            string providerRecordingId,
            CancellationToken ct = default)
        {
            // Agora exposes recording status through Query (resourceId + sid), not a
            // resourceId-only status call, so there is no meaningful implementation
            // here. Return a Result error rather than throwing: a stray call must not
            // take down the request with an unhandled exception. Use QueryAsync instead.
            return Task.FromResult<Result<RecordingStatusResult>>(
                Error.Failure(
                    "Agora.Provider.GetStatusNotSupported",
                    "GetStatusAsync is not supported for Agora; use QueryAsync(resourceId, sid) instead."));
        }

        private StartRecordingRequest BuildStartRequest(string channelName, string uid, string? token)
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
                        Token = token,
                        MaxIdleTime = _settings.RecordingMaxIdleTime,
                        StreamTypes = _settings.RecordingStreamTypes,
                        ChannelType = _settings.RecordingChannelType,
                        VideoStreamType = 0,
                        SubscribeUidGroup = 0,

                        // StreamTypes: 0 = audio only, 1 = video only, 2 = both.
                        // Only subscribe to the tracks we asked Agora to record.
                        SubscribeAudioUids = _settings.RecordingStreamTypes is 0 or 2
                            ? [AllStreams]
                            : null,
                        SubscribeVideoUids = _settings.RecordingStreamTypes is 1 or 2
                            ? [AllStreams]
                            : null
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

        private static StopRecordingRequest BuildStopRequest(string channelName, string recordingUid)
        {
            return new StopRecordingRequest
            {
                Cname = channelName,
                Uid = recordingUid,
                ClientRequest = new StopClientRequest()
            };
        }

        private static uint GenerateRecordingUid()
        {
            return (uint)(HashCode.Combine("bosla-recording", Guid.NewGuid()) & 0x7FFFFFFF);
        }

        /// <summary>
        /// Builds the RTC token the cloud-recording client uses to join the channel.
        /// The channel is secured with an App Certificate, so the recording client is
        /// rejected (and records nothing) unless it presents a token for its own uid.
        /// Returns null for App ID-only channels (no certificate configured), in which
        /// case the token is omitted from the start payload.
        /// </summary>
        private string? BuildRecordingToken(string channelName, uint uid)
        {
            if (string.IsNullOrWhiteSpace(_settings.AppCertificate))
            {
                _logger.LogWarning(
                    "AppCertificate is not configured — starting recording without a token. " +
                    "If the channel is secured, the recording client will fail to join and capture no media.");
                return null;
            }

            // AccessToken2 (RtcTokenBuilder2) takes tokenExpire/privilegeExpire as a
            // DURATION IN SECONDS FROM NOW — not an absolute Unix timestamp. Passing a
            // timestamp produces a token claiming ~56 years of validity, far past
            // Agora's 24-hour maximum. (The older AgoraDynamicKey package used
            // "privilegeExpiredTs" and did take a timestamp; AgoraIO.Token renamed the
            // parameters to "tokenExpire"/"privilegeExpire" precisely because the
            // semantics changed.)
            var expireSeconds = (uint)Math.Max(1, _settings.TokenExpirationMinutes) * 60u;

            var token = RtcTokenBuilder2.buildTokenWithUid(
                _settings.AppId,
                _settings.AppCertificate,
                channelName,
                uid,
                RtcTokenBuilder2.Role.RolePublisher,
                expireSeconds,
                expireSeconds);

            // Diagnostic: the request-body logger redacts the token itself, so log its
            // shape instead — enough to confirm the 007 format and a sane expiry
            // without ever writing the credential to disk.
            _logger.LogInformation(
                "Recording token built: version={Version} length={Length} expireSecondsArg={ExpireSeconds} " +
                "expectedExpiryUtc={ExpiryUtc:o} uid={Uid} channel={Channel}",
                token.Length >= 3 ? token[..3] : "??",
                token.Length,
                expireSeconds,
                DateTimeOffset.UtcNow.AddSeconds(expireSeconds),
                uid,
                channelName);

            return token;
        }

        private static IReadOnlyList<RecordingFileInfo>? ParseFileList(JsonElement serverResponse)
        {
            if (!serverResponse.TryGetProperty("fileList", out var fileList))
            {
                return null;
            }

            // Agora reports the shape of fileList in `fileListMode`:
            //   "string" — composite/mix mode with avFileType ["hls"]: fileList is the
            //              M3U8 key as a plain JSON string (our default configuration).
            //   "json"   — individual mode, or composite with ["hls","mp4"]: an array.
            // GetArrayLength() throws on a JSON string, so the shape must be branched on
            // ValueKind before touching it — otherwise the HLS Stop/Query response (our
            // real production path) crashes with InvalidOperationException.
            if (fileList.ValueKind == JsonValueKind.String)
            {
                var singleFileName = fileList.GetString();
                if (string.IsNullOrEmpty(singleFileName))
                {
                    return null;
                }

                // The stop/query "string" fileList carries no size or slice time; those
                // arrive later on the "uploaded" webhook (or via S3 HeadObject).
                return new List<RecordingFileInfo>
                {
                    new(singleFileName, singleFileName, 0L, null, InferMimeType(singleFileName))
                };
            }

            if (fileList.ValueKind != JsonValueKind.Array ||
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

        /// <summary>
        /// Reads <c>serverResponse.uploadingStatus</c>. Agora returns "uploaded" once
        /// every file is in the configured S3 bucket, "backuped"/"backuping" while some
        /// files are still in Agora's own backup storage (not yet in S3). Anything else
        /// (including a missing field) is treated as Unknown so the caller keeps polling.
        /// </summary>
        private static AgoraUploadingStatus ParseUploadingStatus(JsonElement serverResponse)
        {
            if (!serverResponse.TryGetProperty("uploadingStatus", out var us) ||
                us.ValueKind != JsonValueKind.String)
            {
                return AgoraUploadingStatus.Unknown;
            }

            return us.GetString()?.ToLowerInvariant() switch
            {
                "uploaded" => AgoraUploadingStatus.Uploaded,
                "backuped" => AgoraUploadingStatus.Backuped,
                "backuping" => AgoraUploadingStatus.Backuping,
                _ => AgoraUploadingStatus.Unknown
            };
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