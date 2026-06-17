namespace BoslaPlatform.Application.Interfaces.AI;

public interface IEmbeddingService
{
    Task<string> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}
