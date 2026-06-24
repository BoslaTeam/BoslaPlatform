using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BoslaPlatform.Infrastructure.Settings;

namespace BoslaPlatform.Infrastructure.AI;

    public class EmbeddingAdminService : IEmbeddingAdminService
    {
        private readonly IAppDbContext _db;
        private readonly IEmbeddingService _emb;
        private readonly IVectorStore _vectors;
        private readonly GeminiSettings? _geminiSettings;
        private readonly BoslaPlatform.Application.Interfaces.Authentication.IUser _currentUser;

        public EmbeddingAdminService(IAppDbContext db, IEmbeddingService emb, IVectorStore vectors, IOptions<GeminiSettings>? geminiOpts, BoslaPlatform.Application.Interfaces.Authentication.IUser currentUser)
        {
            _db = db;
            _emb = emb;
            _vectors = vectors;
            _geminiSettings = geminiOpts?.Value;
            _currentUser = currentUser;
        }

    public Task<Result<object>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new
        {
            EmbeddingModel = _geminiSettings?.EmbeddingModel ?? "(not configured)",
            QdrantCollection = "specialists"
        };
        return Task.FromResult(Result<object>.Success(status));
    }

    public async Task<Result<bool>> RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        // Iterate specialists and rebuild embeddings (simple approach)
        var specs = await _db.Set<Domain.Entities.Profile.Specialist>().Include(s => s.User).ToListAsync(cancellationToken);
        foreach (var s in specs)
        {
            var content = string.Join("\n", new[] { s.User?.Bio ?? string.Empty, s.User?.Title ?? string.Empty });
            if (string.IsNullOrWhiteSpace(content)) continue;

            var emb = await _emb.CreateEmbeddingAsync(content, cancellationToken);
            var hash = Guid.NewGuid().ToString();
            await _vectors.StoreEmbeddingAsync(s.Id, emb, _geminiSettings?.EmbeddingModel ?? string.Empty, hash, cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RebuildSelfAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            return Result<bool>.Failure(Error.Unauthorized("Embedding.Unauthenticated", "User is not authenticated."));

        var uid = _currentUser.Id.Value;
        var specialist = await _db.Set<Domain.Entities.Profile.Specialist>().FirstOrDefaultAsync(s => s.UserId == uid, cancellationToken);
        if (specialist == null)
            return Result<bool>.Failure(Error.NotFound("Specialist.NotFound", "Specialist profile not found for current user."));

        var content = string.Join("\n", new[] { specialist.User?.Bio ?? string.Empty, specialist.User?.Title ?? string.Empty });
        if (string.IsNullOrWhiteSpace(content))
            return Result<bool>.Failure(Error.Validation("Embedding.EmptyContent", "No content available to embed for this specialist."));

        var emb = await _emb.CreateEmbeddingAsync(content, cancellationToken);
        var hash = Guid.NewGuid().ToString();
        await _vectors.StoreEmbeddingAsync(specialist.Id, emb, _geminiSettings?.EmbeddingModel ?? string.Empty, hash, cancellationToken);

        return Result<bool>.Success(true);
    }
}
