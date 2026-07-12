using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.Realtime
{
    internal sealed class NoOpRecordingProvider : IRecordingProvider
    {
        public string Name => "NoOp";

        public Task<Result<AcquireResult>> AcquireAsync(
            string channelName,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<AcquireResult>.Success(
                    new AcquireResult("noop-resource-id")));
        }

        public Task<Result<QueryResult>> QueryAsync(
            string providerRecordingId,
            string sid,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Result<QueryResult>.Success(
                    new QueryResult(RecordingStatus.Completed, providerRecordingId, sid)));
        }

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
