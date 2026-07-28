using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    /// <summary>
    /// Abstraction for cloud recording providers (Agora, Zoom, Daily, etc.).
    /// Future providers implement this interface without changing the controller.
    /// </summary>
    public interface IRecordingProvider
    {
        string Name { get; }

        Task<Result<AcquireResult>> AcquireAsync(
            string channelName,
            CancellationToken ct = default);

        Task<Result<QueryResult>> QueryAsync(
            string providerRecordingId,
            string sid,
            CancellationToken ct = default);

        Task<Result<StartRecordingResult>> StartRecordingAsync(
            string channelName,
            CancellationToken ct = default);

        Task<Result<StopRecordingResult>> StopRecordingAsync(
            string channelName,
            string providerRecordingId,
            string providerRecordingSid,
            string recordingUid,
            CancellationToken ct = default);

        Task<Result<RecordingStatusResult>> GetStatusAsync(
            string providerRecordingId,
            CancellationToken ct = default);
    }

    public sealed record AcquireResult(string ResourceId);

    public sealed record QueryResult(
        RecordingStatus Status,
        string ResourceId,
        string Sid,
        IReadOnlyList<RecordingFileInfo>? Files = null,
        RecordingSummary? Summary = null,
        AgoraUploadingStatus UploadingStatus = AgoraUploadingStatus.Unknown);

    public sealed record StartRecordingResult(
        string ProviderRecordingId,
        string ProviderRecordingSid,
        string RecordingUid);

    public sealed record StopRecordingResult(
        string FileUrl,
        int DurationSeconds,
        long FileSizeBytes,
        IReadOnlyList<RecordingFileInfo>? Files = null,
        RecordingSummary? Summary = null,
        AgoraUploadingStatus UploadingStatus = AgoraUploadingStatus.Unknown);

    /// <summary>
    /// Agora's <c>serverResponse.uploadingStatus</c> from Stop/Query. Only
    /// <see cref="Uploaded"/> means every file reached the configured S3 bucket;
    /// <see cref="Backuped"/> means at least one file is still in Agora's backup
    /// storage and will be transferred to S3 later, so it is NOT yet complete.
    /// </summary>
    public enum AgoraUploadingStatus
    {
        Unknown = 0,
        Uploaded,
        Backuped,
        Backuping
    }

    public sealed record RecordingStatusResult(string Status, string? FileUrl);

    public sealed record RecordingFileInfo(
        string FileName,
        string ObjectKey,
        long FileSize,
        DateTime? StartTime,
        string MimeType,
        string? DownloadUrl = null);

    public sealed record RecordingSummary(
        int FileCount,
        long TotalSizeBytes);
}
