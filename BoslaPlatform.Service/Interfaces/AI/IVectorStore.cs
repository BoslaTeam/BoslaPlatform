namespace BoslaPlatform.Application.Interfaces.AI;

public interface IVectorStore
{
    Task StoreEmbeddingAsync(Guid specialistId, string embeddingVector, string model, string contentHash, CancellationToken cancellationToken = default);
    Task<IList<(Guid SpecialistId, float Score)>> SearchSimilarAsync(string queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
