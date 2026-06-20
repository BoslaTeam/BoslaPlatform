using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.BackgroundJobs;

public class QdrantBackfillJob
{
    private readonly IAppDbContext _db;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<QdrantBackfillJob> _logger;

    public QdrantBackfillJob(IAppDbContext db, IVectorStore vectorStore, ILogger<QdrantBackfillJob> logger)
    {
        _db = db;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var all = await _db.Set<SpecialistEmbedding>().ToListAsync(cancellationToken);
        foreach (var e in all)
        {
            try
            {
                await _vectorStore.StoreEmbeddingAsync(e.SpecialistId, e.EmbeddingVector, e.EmbeddingModel, e.ContentHash, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert embedding for {SpecialistId}", e.SpecialistId);
            }
        }
    }
}
