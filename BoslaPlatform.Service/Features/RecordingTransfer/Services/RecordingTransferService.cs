using System.Diagnostics;
using System.Security.Cryptography;
using BoslaPlatform.Application.Features.RecordingTransfer.Dtos;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Videos;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.RecordingTransfer.Services
{
    public sealed class RecordingTransferService
    {
        private readonly IRecordingProvider _recordingProvider;
        private readonly IObjectStorage _objectStorage;
        private readonly IAgoraRecordingDownloader _downloader;
        private readonly IRecordingStorageSettings _storageSettings;
        private readonly IRecordingMetrics _metrics;
        private readonly IAppDbContext _context;
        private readonly ILogger<RecordingTransferService> _logger;

        public RecordingTransferService(
            IRecordingProvider recordingProvider,
            IObjectStorage objectStorage,
            IAgoraRecordingDownloader downloader,
            IRecordingStorageSettings storageSettings,
            IRecordingMetrics metrics,
            IAppDbContext context,
            ILogger<RecordingTransferService> logger)
        {
            _recordingProvider = recordingProvider;
            _objectStorage = objectStorage;
            _downloader = downloader;
            _storageSettings = storageSettings;
            _metrics = metrics;
            _context = context;
            _logger = logger;
        }

        public async Task TransferRecordingAsync(
            Guid sessionId,
            string resourceId,
            string sid,
            CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();
            _metrics.IncrementActiveUploads();

            _logger.LogInformation(
                "Upload Started. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}",
                sessionId, resourceId, sid);

            try
            {
                var session = await _context.VideoSessions
                    .FirstOrDefaultAsync(x => x.Id == sessionId, ct);

                if (session is null)
                {
                    _logger.LogWarning("Session {VideoSessionId} not found for recording transfer", sessionId);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_storageSettings.BucketName))
                {
                    await FailTransferAsync(session, "Recording storage bucket is not configured.", ct);
                    return;
                }

                session.MarkUploadPending();
                await _context.SaveChangesAsync(ct);

                var queryResult = await _recordingProvider.QueryAsync(resourceId, sid, ct);

                if (queryResult.IsError)
                {
                    _logger.LogWarning(
                        "Failed to query recording for session {VideoSessionId}: {Errors}",
                        sessionId, string.Join("; ", queryResult.Errors.Select(e => e.Description)));

                    await FailTransferAsync(session, queryResult.Errors[0].Description, ct);
                    return;
                }

                var files = queryResult.Value.Files;
                if (files is null || files.Count == 0)
                {
                    _logger.LogWarning(
                        "No recording files found for session {VideoSessionId}", sessionId);

                    await FailTransferAsync(session, "No recording files available from provider.", ct);
                    return;
                }

                var uploadedFiles = new List<UploadedRecordingFile>();
                var totalBytes = 0L;

                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    AgoraRecordingDownloadResult? download = null;
                    var objectKey = BuildObjectKey(sessionId, file);

                    try
                    {
                        session.MarkUploading();
                        await _context.SaveChangesAsync(ct);

                        var downloadResult = await _downloader.DownloadAsync(
                            sessionId,
                            resourceId,
                            sid,
                            file,
                            i,
                            ct);

                        if (downloadResult.IsError)
                        {
                            await FailTransferAsync(session, downloadResult.Errors[0].Description, ct);
                            return;
                        }

                        download = downloadResult.Value;
                        var checksumSha256 = await ComputeSha256Async(download.TempFilePath, ct);

                        var uploadStopwatch = Stopwatch.StartNew();
                        _logger.LogInformation(
                            "Upload Started. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}, ContentLength={ContentLength}, UploadAttempts={UploadAttempts}",
                            sessionId, resourceId, sid, objectKey, download.ContentLength, session.UploadAttempts);

                        var uploadResult = await ExecuteWithRetryAsync(
                            async () =>
                            {
                                await using var fileStream = new FileStream(
                                    download.TempFilePath,
                                    FileMode.Open,
                                    FileAccess.Read,
                                    FileShare.Read,
                                    81920,
                                    useAsync: true);

                                var uploadRequest = new UploadObjectRequest(
                                    BucketName: _storageSettings.BucketName,
                                    ObjectKey: objectKey,
                                    Content: fileStream,
                                    ContentType: download.ContentType,
                                    ContentLength: download.ContentLength);

                                return await _objectStorage.UploadAsync(uploadRequest, ct);
                            },
                            result => result.IsError,
                            session,
                            "Upload",
                            sessionId,
                            resourceId,
                            sid,
                            objectKey,
                            download.ContentLength,
                            ct);

                        uploadStopwatch.Stop();

                        if (uploadResult.IsError)
                        {
                            await FailTransferAsync(session, uploadResult.Errors[0].Description, ct);
                            return;
                        }

                        _logger.LogInformation(
                            "Upload Completed. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}, ContentLength={ContentLength}, Duration={Duration}, UploadAttempts={UploadAttempts}",
                            sessionId, resourceId, sid, objectKey, download.ContentLength, uploadStopwatch.Elapsed, session.UploadAttempts);

                        _logger.LogInformation(
                            "Verification Started. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}",
                            sessionId, resourceId, sid, objectKey);

                        var verifyResult = await ExecuteWithRetryAsync(
                            () => _objectStorage.ExistsAsync(_storageSettings.BucketName, objectKey, ct),
                            result => result.IsError || !result.Value,
                            session,
                            "Verification",
                            sessionId,
                            resourceId,
                            sid,
                            objectKey,
                            download.ContentLength,
                            ct);

                        if (verifyResult.IsError || !verifyResult.Value)
                        {
                            await FailTransferAsync(
                                session,
                                verifyResult.IsError
                                    ? verifyResult.Errors[0].Description
                                    : "Uploaded object was not found during verification.",
                                ct);
                            return;
                        }

                        _logger.LogInformation(
                            "Verification Completed. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}, ContentLength={ContentLength}",
                            sessionId, resourceId, sid, objectKey, download.ContentLength);

                        uploadedFiles.Add(new UploadedRecordingFile(
                            file,
                            uploadResult.Value,
                            objectKey,
                            download.ContentType,
                            download.ContentLength,
                            checksumSha256));

                        totalBytes += download.ContentLength;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Transfer failed for file {FileName} in session {VideoSessionId}",
                            file.FileName, sessionId);

                        await FailTransferAsync(session, ex.Message, ct);
                        return;
                    }
                    finally
                    {
                        if (download is not null)
                        {
                            DeleteTempFile(download.TempFilePath, sessionId, objectKey);
                        }
                    }
                }

                var playbackFile = SelectPlaybackFile(uploadedFiles);
                stopwatch.Stop();

                session.MarkUploadSucceeded(
                    _storageSettings.Provider,
                    _storageSettings.BucketName,
                    playbackFile.ObjectKey,
                    playbackFile.ContentType,
                    playbackFile.ContentLength,
                    checksumSha256: playbackFile.ChecksumSha256,
                    versionId: playbackFile.UploadResponse.VersionId,
                    etag: playbackFile.UploadResponse.ETag);

                session.AddDomainEvent(new RecordingUploadedEvent(
                    sessionId,
                    playbackFile.ObjectKey,
                    _storageSettings.BucketName,
                    totalBytes,
                    stopwatch.Elapsed));

                await _context.SaveChangesAsync(ct);

                _metrics.RecordUploadDuration(stopwatch.Elapsed);
                _metrics.RecordUploadSuccess(totalBytes, stopwatch.Elapsed);

                _logger.LogInformation(
                    "Upload Completed. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}, FileCount={FileCount}, ContentLength={ContentLength}, Duration={Duration}, UploadAttempts={UploadAttempts}, UploadStatus={UploadStatus}",
                    sessionId, resourceId, sid, playbackFile.ObjectKey, uploadedFiles.Count, totalBytes, stopwatch.Elapsed, session.UploadAttempts, UploadStatus.Uploaded);
            }
            finally
            {
                _metrics.DecrementActiveUploads();
            }
        }

        private async Task<Result<T>> ExecuteWithRetryAsync<T>(
            Func<Task<Result<T>>> operation,
            Func<Result<T>, bool> shouldRetry,
            VideoSession session,
            string operationName,
            Guid sessionId,
            string resourceId,
            string sid,
            string objectKey,
            long contentLength,
            CancellationToken ct)
        {
            var attempts = Math.Max(1, _storageSettings.MaxRetryAttempts);
            Result<T>? lastResult = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                lastResult = await operation();
                if (!shouldRetry(lastResult))
                {
                    return lastResult;
                }

                if (attempt == attempts)
                {
                    return lastResult;
                }

                session.MarkUploadRetrying();
                _metrics.RecordRetry();
                await _context.SaveChangesAsync(ct);

                var delay = GetRetryDelay(attempt);
                _logger.LogWarning(
                    "{Operation} retry scheduled. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, ObjectKey={ObjectKey}, ContentLength={ContentLength}, Attempt={Attempt}, MaxAttempts={MaxAttempts}, Delay={Delay}",
                    operationName, sessionId, resourceId, sid, objectKey, contentLength, attempt, attempts, delay);

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, ct);
                }
            }

            return lastResult ?? Error.Failure(
                "RecordingTransfer.RetryFailed",
                $"{operationName} failed before it could be attempted.");
        }

        private TimeSpan GetRetryDelay(int attempt)
        {
            var baseSeconds = Math.Max(0, _storageSettings.RetryBaseDelaySeconds);
            return TimeSpan.FromSeconds(baseSeconds * Math.Pow(2, attempt - 1));
        }

        private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            var hash = await SHA256.HashDataAsync(stream, ct);
            return Convert.ToHexStringLower(hash);
        }

        private static UploadedRecordingFile SelectPlaybackFile(IReadOnlyList<UploadedRecordingFile> files)
        {
            return files
                .OrderBy(GetPlaybackPriority)
                .ThenBy(x => x.SourceFile.FileName, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int GetPlaybackPriority(UploadedRecordingFile file)
        {
            var extension = Path.GetExtension(file.SourceFile.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            if (extension == ".mp4" || contentType == "video/mp4") return 0;
            if (extension == ".m3u8" || contentType.Contains("mpegurl")) return 1;
            if (extension == ".ts" || contentType == "video/mp2t") return 2;

            return 3;
        }

        private static string BuildObjectKey(Guid sessionId, RecordingFileInfo file)
        {
            var rawKey = !string.IsNullOrWhiteSpace(file.ObjectKey)
                ? file.ObjectKey
                : file.FileName;

            var normalized = rawKey
                .Replace('\\', '/')
                .Trim('/');

            return normalized.StartsWith(sessionId.ToString(), StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"{sessionId}/{normalized}";
        }

        private void DeleteTempFile(string tempPath, Guid sessionId, string objectKey)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    _logger.LogDebug(
                        "Temporary file deleted. VideoSessionId={VideoSessionId}, ObjectKey={ObjectKey}, TempPath={TempPath}",
                        sessionId, objectKey, tempPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Temporary file cleanup failed. VideoSessionId={VideoSessionId}, ObjectKey={ObjectKey}, TempPath={TempPath}",
                    sessionId, objectKey, tempPath);
            }
        }

        private async Task FailTransferAsync(VideoSession session, string error, CancellationToken ct)
        {
            session.MarkUploadFailed(error);
            session.AddDomainEvent(new RecordingUploadFailedEvent(
                session.Id,
                null,
                null,
                session.LastUploadError ?? error,
                session.UploadAttempts));

            _metrics.RecordUploadFailure("RecordingTransfer.Failed");

            _logger.LogWarning(
                "Upload Failed. VideoSessionId={VideoSessionId}, Error={Error}, UploadAttempts={UploadAttempts}, UploadStatus={UploadStatus}",
                session.Id, error, session.UploadAttempts, UploadStatus.Failed);

            await _context.SaveChangesAsync(ct);
        }

        private sealed record UploadedRecordingFile(
            RecordingFileInfo SourceFile,
            UploadObjectResponse UploadResponse,
            string ObjectKey,
            string ContentType,
            long ContentLength,
            string ChecksumSha256);
    }
}
