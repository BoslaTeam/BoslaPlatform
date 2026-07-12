using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video;

public interface IAgoraRecordingDownloader
{
    Task<Result<AgoraRecordingDownloadResult>> DownloadAsync(
        Guid videoSessionId,
        string resourceId,
        string sid,
        RecordingFileInfo file,
        int fileIndex,
        CancellationToken ct = default);
}

public sealed record AgoraRecordingDownloadResult(
    string TempFilePath,
    string FileName,
    string ContentType,
    long ContentLength);
