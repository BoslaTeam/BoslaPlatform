using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.Realtime
{
    internal sealed class NoOpRecordingProvider : IRecordingProvider
    {
        public string Name => "NoOp";

        public Task<Result<StartRecordingResult>> StartRecordingAsync(
            string channelName,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<StartRecordingResult>.Success(
                    new StartRecordingResult("noop-sid")));
        }

        public Task<Result<StopRecordingResult>> StopRecordingAsync(
            string channelName,
            string providerRecordingId,
            string? providerMetadata = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<StopRecordingResult>.Success(
                    new StopRecordingResult(string.Empty, 0, 0)));
        }

        public Task<Result<RecordingStatusResult>> GetStatusAsync(
            string providerRecordingId,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<RecordingStatusResult>.Success(
                    new RecordingStatusResult("completed", null)));
        }
    }
}
