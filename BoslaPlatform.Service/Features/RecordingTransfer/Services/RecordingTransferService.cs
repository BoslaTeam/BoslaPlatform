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
        private readonly IFileDownloader _fileDownloader;
        private readonly IAppDbContext _context;
        private readonly ILogger<RecordingTransferService> _logger;

        public RecordingTransferService(
            IRecordingProvider recordingProvider,
            IObjectStorage objectStorage,
            IFileDownloader fileDownloader,
            IAppDbContext context,
            ILogger<RecordingTransferService> logger)
        {
            _recordingProvider = recordingProvider;
            _objectStorage = objectStorage;
            _fileDownloader = fileDownloader;
            _context = context;
            _logger = logger;
        }

        public async Task TransferRecordingAsync(
            Guid sessionId,
            string resourceId,
            string sid,
            CancellationToken ct)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "Upload started for session {SessionId}, resourceId={ResourceId}, sid={Sid}",
                sessionId, resourceId, sid);

            var session = await _context.VideoSessions
                .FirstOrDefaultAsync(x => x.Id == sessionId, ct);

            if (session is null)
            {
                _logger.LogWarning("Session {SessionId} not found for recording transfer", sessionId);
                return;
            }

            session.MarkUploadPending();
            await _context.SaveChangesAsync(ct);

            var queryResult = await _recordingProvider.QueryAsync(resourceId, sid, ct);

            if (queryResult.IsError)
            {
                _logger.LogWarning(
                    "Failed to query recording for session {SessionId}: {Errors}",
                    sessionId, string.Join("; ", queryResult.Errors.Select(e => e.Description)));

                await FailTransferAsync(session, queryResult.Errors[0].Description, ct);
                return;
            }

            var files = queryResult.Value.Files;
            if (files is null || files.Count == 0)
            {
                _logger.LogWarning(
                    "No recording files found for session {SessionId}", sessionId);

                await FailTransferAsync(session, "No recording files available from provider.", ct);
                return;
            }

            string? firstObjectKey = null;
            string? firstETag = null;
            string? firstVersionId = null;
            string? checksumSha256 = null;
            var uploadedCount = 0;
            var totalBytes = 0L;

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var tempPath = Path.Combine(Path.GetTempPath(), $"{sessionId}_{i}_{Guid.NewGuid()}_{file.FileName}");

                try
                {
                    session.MarkUploading();
                    await _context.SaveChangesAsync(ct);

                    var downloadUrl = file.DownloadUrl ?? file.FileName;
                    await _fileDownloader.DownloadAsync(downloadUrl, tempPath, ct);

                    var fileSha256 = await ComputeSha256Async(tempPath, ct);

                    var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        var objectKey = $"{sessionId}/{file.FileName}";
                        var uploadRequest = new UploadObjectRequest(
                            BucketName: "recordings",
                            ObjectKey: objectKey,
                            Content: fileStream,
                            ContentType: file.MimeType,
                            ContentLength: file.FileSize);

                        var uploadResult = await _objectStorage.UploadAsync(uploadRequest, ct);

                        if (uploadResult.IsError)
                        {
                            _logger.LogWarning(
                                "Failed to upload file {FileName} for session {SessionId}: {Error}",
                                file.FileName, sessionId, uploadResult.Errors[0].Description);

                            await FailTransferAsync(session, uploadResult.Errors[0].Description, ct);
                            return;
                        }

                        firstObjectKey ??= objectKey;
                        firstETag ??= uploadResult.Value.ETag;
                        firstVersionId ??= uploadResult.Value.VersionId;
                        checksumSha256 ??= fileSha256;
                        uploadedCount++;
                        totalBytes += file.FileSize;

                        _logger.LogInformation(
                            "Uploaded file {FileName} ({Size} bytes) to {Bucket}/{Key} for session {SessionId}",
                            file.FileName, file.FileSize, "recordings", objectKey, sessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Transfer failed for file {FileName} in session {SessionId}",
                        file.FileName, sessionId);

                    await FailTransferAsync(session, ex.Message, ct);
                    return;
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                        _logger.LogDebug("Temporary file {TempPath} deleted", tempPath);
                    }
                }
            }

            stopwatch.Stop();
            session.MarkUploadSucceeded(
                StorageProvider.CloudflareR2,
                "recordings",
                firstObjectKey ?? string.Empty,
                "application/octet-stream",
                totalBytes,
                checksumSha256: checksumSha256,
                versionId: firstVersionId,
                etag: firstETag);
            await _context.SaveChangesAsync(ct);

            session.AddDomainEvent(new RecordingUploadedEvent(
                sessionId,
                firstObjectKey ?? string.Empty,
                "recordings",
                totalBytes,
                stopwatch.Elapsed));

            _logger.LogInformation(
                "Upload completed for session {SessionId}: {FileCount} files, {TotalBytes} bytes, duration={Duration}, status={Status}",
                sessionId, uploadedCount, totalBytes, stopwatch.Elapsed, UploadStatus.Uploaded);
        }

        private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            var hash = await SHA256.HashDataAsync(stream, ct);
            return Convert.ToHexStringLower(hash);
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

            _logger.LogWarning(
                "Upload failed for session {SessionId}: {Error}, attempts={Attempts}, status={Status}",
                session.Id, error, session.UploadAttempts, UploadStatus.Failed);

            await _context.SaveChangesAsync(ct);
        }
    }
}