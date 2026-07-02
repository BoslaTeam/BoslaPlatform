using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface IRecordingProvider
    {
        string Name { get; }

        Task<Result<StartRecordingResult>> StartRecordingAsync(
            string channelName,
            CancellationToken ct = default);

        Task<Result<StopRecordingResult>> StopRecordingAsync(
            string channelName,
            string providerRecordingId,
            string? providerMetadata = null,
            CancellationToken ct = default);

        Task<Result<RecordingStatusResult>> GetStatusAsync(
            string providerRecordingId,
            CancellationToken ct = default);
    }

    public sealed record StartRecordingResult(string ProviderRecordingId, string? ProviderMetadata = null);
    public sealed record StopRecordingResult(string FileUrl, int DurationSeconds, long FileSizeBytes);
    public sealed record RecordingStatusResult(string Status, string? FileUrl);
}
