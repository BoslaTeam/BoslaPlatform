using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.Realtime
{
    internal sealed class NoOpSTTProvider : ISTTProvider
    {
        public Task<Result> StopSTTAsync(
            string channelName,
            CancellationToken ct = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
}
