using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video
{
    public interface ISTTProvider
    {
        Task<Result> StopSTTAsync(
            string channelName,
            CancellationToken ct = default);
    }
}
