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
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(emb);
            await _vectors.StoreEmbeddingAsync(s.Id, embeddingJson, _geminiSettings?.EmbeddingModel ?? string.Empty, hash, cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RebuildSelfAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            return Result<bool>.Failure(Error.Unauthorized("Embedding.Unauthenticated", "User is not authenticated."));

        var uid = _currentUser.Id.Value;
        var specialist = await _db.Set<Domain.Entities.Profile.Specialist>()
            .Include(s => s.User)
            .Include(s => s.SpecialistExpertise)!
                .ThenInclude(se => se.Expertise)
            .Include(s => s.SpecialistIndustries)!
                .ThenInclude(si => si.Industry)
            .Include(s => s.SpecialistSkills)!
                .ThenInclude(ss => ss.Skill)
            .Include(s => s.SpecialistTools)!
                .ThenInclude(st => st.Tool)
            .Include(s => s.Experiences)
            .FirstOrDefaultAsync(s => s.UserId == uid, cancellationToken);
        if (specialist == null)
            return Result<bool>.Failure(Error.NotFound("Specialist.NotFound", "Specialist profile not found for current user."));

        var contentParts = new List<string>();

        // Add user bio and title
        if (!string.IsNullOrWhiteSpace(specialist.User?.Bio))
            contentParts.Add(specialist.User.Bio);

        if (!string.IsNullOrWhiteSpace(specialist.User?.Title))
            contentParts.Add(specialist.User.Title);

        // Add experience information
        contentParts.Add($"Experience Level: {specialist.ExperienceLevel}");
        contentParts.Add($"Years of Experience: {specialist.ExperienceYears}");

        // Add expertise areas
        if (specialist.SpecialistExpertise?.Any() == true)
        {
            var expertiseNames = specialist.SpecialistExpertise
                .Where(se => se.Expertise != null && !string.IsNullOrWhiteSpace(se.Expertise.Name))
                .Select(se => se.Expertise!.Name)
                .ToList();
            if (expertiseNames.Any())
                contentParts.Add($"Expertise: {string.Join(", ", expertiseNames)}");
        }

        // Add industries
        if (specialist.SpecialistIndustries?.Any() == true)
        {
            var industryNames = specialist.SpecialistIndustries
                .Where(si => si.Industry != null && !string.IsNullOrWhiteSpace(si.Industry.Name))
                .Select(si => si.Industry!.Name)
                .ToList();
            if (industryNames.Any())
                contentParts.Add($"Industries: {string.Join(", ", industryNames)}");
        }

        // Add skills
        if (specialist.SpecialistSkills?.Any() == true)
        {
            var skillNames = specialist.SpecialistSkills
                .Where(ss => ss.Skill != null && !string.IsNullOrWhiteSpace(ss.Skill.Name))
                .Select(ss => ss.Skill!.Name)
                .ToList();
            if (skillNames.Any())
                contentParts.Add($"Skills: {string.Join(", ", skillNames)}");
        }

        // Add tools
        if (specialist.SpecialistTools?.Any() == true)
        {
            var toolNames = specialist.SpecialistTools
                .Where(st => st.Tool != null && !string.IsNullOrWhiteSpace(st.Tool.Name))
                .Select(st => st.Tool!.Name)
                .ToList();
            if (toolNames.Any())
                contentParts.Add($"Tools: {string.Join(", ", toolNames)}");
        }

        // Add experiences/work history
        if (specialist.Experiences?.Any() == true)
        {
            var experienceDescriptions = specialist.Experiences
                .Where(e => !string.IsNullOrWhiteSpace(e.Description))
                .Select(e => e.Description)
                .ToList();
            if (experienceDescriptions.Any())
                contentParts.Add($"Work Experience: {string.Join("; ", experienceDescriptions)}");
        }

        var content = string.Join("\n", contentParts.Where(cp => !string.IsNullOrWhiteSpace(cp)));
        if (string.IsNullOrWhiteSpace(content))
            return Result<bool>.Failure(Error.Validation("Embedding.EmptyContent", "No content available to embed for this specialist."));

        var embeddingJson = await _emb.CreateEmbeddingAsync(content, cancellationToken);
        if (string.IsNullOrWhiteSpace(embeddingJson) || embeddingJson == "[]")
            return Result<bool>.Failure(Error.Validation("Embedding.Failed", "Failed to create embedding from content."));

        var hash = Guid.NewGuid().ToString();
        await _vectors.StoreEmbeddingAsync(specialist.Id, embeddingJson, _geminiSettings?.EmbeddingModel ?? string.Empty, hash, cancellationToken);

        return Result<bool>.Success(true);
    }
}
