using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Video;

public interface ISTTProvider
{
    string Name { get; }

    Task<Result<StartSTTResult>> StartSTTAsync(
        string channelName,
        string languageCode,
        CancellationToken ct = default);
}

public sealed record StartSTTResult(string ProviderSTTId);
