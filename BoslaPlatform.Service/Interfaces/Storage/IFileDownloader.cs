namespace BoslaPlatform.Application.Interfaces.Storage;

public interface IFileDownloader
{
    Task DownloadAsync(string sourceUrl, string destinationPath, CancellationToken ct);
}