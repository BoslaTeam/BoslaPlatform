using BoslaPlatform.Application.Interfaces.Storage;

namespace BoslaPlatform.Infrastructure.Storage;

public sealed class HttpClientFileDownloader : IFileDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientFileDownloader(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task DownloadAsync(string sourceUrl, string destinationPath, CancellationToken ct)
    {
        using var httpClient = _httpClientFactory.CreateClient("RecordingDownload");
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        using var response = await httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
        await using var targetStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await sourceStream.CopyToAsync(targetStream, ct);
    }
}