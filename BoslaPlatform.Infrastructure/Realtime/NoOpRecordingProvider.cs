using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.Realtime
{
    internal sealed class NoOpRecordingProvider : IRecordingProvider
    {
        public string Name => "NoOp";

        public Task<Result<AcquireRecordingResponse>> AcquireAsync(
            string channelName,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<AcquireRecordingResponse>.Success(
                    new AcquireRecordingResponse("noop-resource-id")));
        }

        public Task<Result<ProviderStartRecordingResponse>> StartAsync(
            string channelName,
            string resourceId,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<ProviderStartRecordingResponse>.Success(
                    new ProviderStartRecordingResponse("noop-sid")));
        }

        public Task<Result<ProviderStopRecordingResponse>> StopAsync(
            string channelName,
            string resourceId,
            string sid,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<ProviderStopRecordingResponse>.Success(
                    new ProviderStopRecordingResponse(string.Empty, 0, 0)));
        }

        public Task<Result<QueryRecordingResponse>> QueryAsync(
            string resourceId,
            string sid,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<QueryRecordingResponse>.Success(
                    new QueryRecordingResponse("completed", null)));
        }
    }
}
