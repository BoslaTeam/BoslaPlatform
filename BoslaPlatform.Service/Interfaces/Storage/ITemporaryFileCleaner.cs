namespace BoslaPlatform.Application.Interfaces.Storage;

public interface ITemporaryFileCleaner
{
    Task CleanupAsync(TimeSpan? retention = null, CancellationToken ct = default);
}