using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Infrastructure.STT;

internal sealed class NoOpSTTProvider : ISTTProvider
{
    public string Name => "NoOp";

    public Task<Result<StartSTTResult>> StartSTTAsync(
        string channelName,
        string languageCode,
        CancellationToken ct = default)
    {
        return Task.FromResult(
            Result<StartSTTResult>.Success(
                new StartSTTResult("noop-stt-id")));
    }
}
