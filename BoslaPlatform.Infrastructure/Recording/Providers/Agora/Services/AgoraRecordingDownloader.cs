using System.Diagnostics;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;

public sealed class AgoraRecordingDownloader : IAgoraRecordingDownloader
{
    public const string HttpClientName = "AgoraRecordingDownload";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgoraRecordingDownloader> _logger;

    public AgoraRecordingDownloader(
        IHttpClientFactory httpClientFactory,
        ILogger<AgoraRecordingDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<AgoraRecordingDownloadResult>> DownloadAsync(
        Guid videoSessionId,
        string resourceId,
        string sid,
        RecordingFileInfo file,
        int fileIndex,
        CancellationToken ct = default)
    {
        var sourceUrl = file.DownloadUrl ?? file.ObjectKey ?? file.FileName;
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return Error.Validation(
                "AgoraRecording.DownloadUrlRequired",
                "Recording file download URL is required.");
        }

        var safeFileName = SanitizeFileName(file.FileName, fileIndex);
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"bosla_agora_recording_{videoSessionId:N}_{fileIndex}_{Guid.NewGuid():N}_{safeFileName}");

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Download Started. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, FileName={FileName}, ContentLength={ContentLength}",
            videoSessionId, resourceId, sid, file.FileName, file.FileSize);

        try
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.GetAsync(
                sourceUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            await using (var sourceStream = await response.Content.ReadAsStreamAsync(ct))
            await using (var targetStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await sourceStream.CopyToAsync(targetStream, ct);
            }

            var contentLength = new FileInfo(tempPath).Length;
            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? file.MimeType
                ?? "application/octet-stream";

            sw.Stop();
            _logger.LogInformation(
                "Download Completed. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, FileName={FileName}, ContentLength={ContentLength}, Duration={Duration}",
                videoSessionId, resourceId, sid, file.FileName, contentLength, sw.Elapsed);

            return new AgoraRecordingDownloadResult(
                tempPath,
                file.FileName,
                contentType,
                contentLength);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            TryDeleteTempFile(tempPath);

            _logger.LogError(
                ex,
                "Download Failed. VideoSessionId={VideoSessionId}, ResourceId={ResourceId}, SID={Sid}, FileName={FileName}, Duration={Duration}",
                videoSessionId, resourceId, sid, file.FileName, sw.Elapsed);

            return Error.Failure(
                "AgoraRecording.DownloadFailed",
                ex.Message);
        }
    }

    private static string SanitizeFileName(string? fileName, int fileIndex)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"recording-{fileIndex}.tmp";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort: background temp cleanup will remove bosla_* leftovers.
        }
    }
}
