using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BoslaPlatform.Infrastructure.AI.Gemini;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly GeminiSettings _settings;
    private readonly GeminiHttpClient _client;

    public GeminiEmbeddingService(IOptions<GeminiSettings> options, GeminiHttpClient client)
    {
        _settings = options.Value;
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

        public async Task<string> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return "[]";

        // Use EmbeddingBatcher to control concurrency and simpler parsing
        var batcher = new EmbeddingBatcher(Options.Create(_settings), _client, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmbeddingBatcher>.Instance);
        var vector = await Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiEmbeddingService>.Instance.TrackRequestAsync("Gemini.Embed", () => batcher.CreateEmbeddingAsync(input, cancellationToken));
        return JsonSerializer.Serialize(vector);
    }
}
