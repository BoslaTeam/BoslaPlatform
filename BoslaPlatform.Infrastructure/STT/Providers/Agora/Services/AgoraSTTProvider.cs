using System.Diagnostics;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Shared;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.STT.Providers.Agora.Services;

internal sealed class AgoraSTTProvider : ISTTProvider
{
    private readonly AgoraCloudSTTApiClient _client;
    private readonly ILogger<AgoraSTTProvider> _logger;

    public AgoraSTTProvider(
        AgoraCloudSTTApiClient client,
        ILogger<AgoraSTTProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "Agora";

    public async Task<Result<StartSTTResult>> StartSTTAsync(
        string channelName,
        string languageCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            return Error.Validation("Agora.STT.Provider.MissingChannelName", "Channel name is required.");

        if (string.IsNullOrWhiteSpace(languageCode))
            return Error.Validation("Agora.STT.Provider.MissingLanguageCode", "Language code is required.");

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "STT start requested for channel {ChannelName}, language {Language}",
            channelName, languageCode);

        var result = await _client.StartTaskAsync(channelName, languageCode, ct);

        if (result.IsError)
        {
            _logger.LogWarning(
                "STT start failed for channel {ChannelName} after {ElapsedMs}ms",
                channelName, stopwatch.ElapsedMilliseconds);
            return result.Errors;
        }

        var response = result.Value;

        _logger.LogInformation(
            "STT started for channel {ChannelName} in {ElapsedMs}ms",
            channelName, stopwatch.ElapsedMilliseconds);

        return Result<StartSTTResult>.Success(
            new StartSTTResult(response.AgentId));
    }
}
