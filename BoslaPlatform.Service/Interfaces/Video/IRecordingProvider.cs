using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface IRecordingProvider
    {
        string Name { get; }

        Task<Result<AcquireRecordingResponse>> AcquireAsync(
            string channelName,
            CancellationToken ct = default);

        Task<Result<ProviderStartRecordingResponse>> StartAsync(
            string channelName,
            string resourceId,
            CancellationToken ct = default);

        Task<Result<ProviderStopRecordingResponse>> StopAsync(
            string channelName,
            string resourceId,
            string sid,
            CancellationToken ct = default);

        Task<Result<QueryRecordingResponse>> QueryAsync(
            string resourceId,
            string sid,
            CancellationToken ct = default);
    }

    public sealed record AcquireRecordingResponse(string ResourceId);
    public sealed record ProviderStartRecordingResponse(string Sid);
    public sealed record ProviderStopRecordingResponse(string FileUrl, int Duration, long FileSize);
    public sealed record QueryRecordingResponse(string Status, string? FileUrl);
}
