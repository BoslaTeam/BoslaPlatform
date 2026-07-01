using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Infrastructure.AI.Tokenizers;
using BoslaPlatform.Service.Features.AI.Responses;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Application.Interfaces.Authentication;
namespace BoslaPlatform.Infrastructure.AI;

public class AiSearchService : IAiSearchService
{
    private readonly IEmbeddingService _emb;
    private readonly IVectorStore _vectors;
    private readonly IChatService _chat;
    private readonly IAppDbContext _db;
    private readonly IUser _currentUser;
    private readonly ITokenizer _tokenizer;

    public AiSearchService(IEmbeddingService emb, IVectorStore vectors, IChatService chat, IAppDbContext db, IUser currentUser, ITokenizer tokenizer)
    {
        _emb = emb;
        _vectors = vectors;
        _chat = chat;
        _db = db;
        _currentUser = currentUser;
        _tokenizer = tokenizer;
    }

    public async Task<List<SearchHistoryItemDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Return recent search interactions for the current user
        var uid = _currentUser.Id ?? Guid.Empty;
        var q = _db.Set<SearchInteraction>().Where(si => si.UserId == uid).OrderByDescending(si => si.CreatedAtUtc).Take(50);
        var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(q, cancellationToken);
        return list.Select(si => new SearchHistoryItemDto
        {
            Id = si.Id,
            CreatedAtUtc = si.CreatedAtUtc,
            RawQuery = si.RawQuery,
            ResultSpecialistIds = si.ResultSpecialistIds,
            ClickedSpecialistId = si.ClickedSpecialistId,
            WasHelpful = si.WasHelpful
        }).ToList();
    }

    public async Task RecordFeedbackAsync(Guid searchInteractionId, FeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var si = await _db.Set<SearchInteraction>().FindAsync(new object[] { searchInteractionId }, cancellationToken);
        if (si == null) return;
        if (request.ClickedSpecialistId.HasValue)
        {
            si.ClickedSpecialistId = request.ClickedSpecialistId.Value;
        }
        si.WasHelpful = request.WasHelpful;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiSearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var resp = new AiSearchResponse();

        // 1. embed query
        var qEmb = await _emb.CreateEmbeddingAsync(request.Query, cancellationToken);

        // 2. vector search
        var hits = await _vectors.SearchSimilarAsync(qEmb, request.TopK, cancellationToken);

        // 3. load specialists and build snippets (batch load to include related data)
        var ids = hits.Select(h => h.SpecialistId).ToList();
        var specialistQuery = _db.Set<Specialist>().Where(s => ids.Contains(s.Id));
        List<Specialist> specialists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync<Specialist>(specialistQuery, cancellationToken);

        // Load related users
        var userIds = specialists.Select(s => s.UserId).Distinct().ToList();
        var userQuery = _db.Set<BoslaPlatform.Domain.Entities.User>().Where(u => userIds.Contains(u.Id));
        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync<BoslaPlatform.Domain.Entities.User>(userQuery, cancellationToken);
        foreach (var s in specialists)
        {
            s.User = users.FirstOrDefault(u => u.Id == s.UserId) ?? s.User;
        }

        // Load related SpecialistSkills and Experiences
        var specialistIds = specialists.Select(s => s.Id).ToList();
        var skillsQuery = _db.Set<Domain.Models.Junctions.SpecialistSkill>().Where(ss => specialistIds.Contains(ss.SpecialistId));
        var skills = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync<Domain.Models.Junctions.SpecialistSkill>(skillsQuery, cancellationToken);
        var experiencesQuery = _db.Set<Domain.Models.Profile.SpecialistExperience>().Where(e => specialistIds.Contains(e.SpecialistId));
        var experiences = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync<Domain.Models.Profile.SpecialistExperience>(experiencesQuery, cancellationToken);
        foreach (var s in specialists)
        {
            s.SpecialistSkills = skills.Where(k => k.SpecialistId == s.Id).ToList();
            s.Experiences = experiences.Where(e => e.SpecialistId == s.Id).ToList();
        }

        var results = new List<SearchResultDto>();
        foreach (var (id, score) in hits)
        {
            var specialist = specialists.FirstOrDefault(s => s.Id == id);
            if (specialist == null) continue;

            var bio = specialist.User?.Bio ?? string.Empty;
            var title = specialist.User?.Title ?? string.Empty;
            var skillNames = specialist.SpecialistSkills?.Select(ss => ss.Skill?.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string?>();
            var expSummary = specialist.Experiences?.Take(3).Select(e => e.JobTitle ?? string.Empty) ?? Enumerable.Empty<string>();

            var snippetParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(title)) snippetParts.Add(title);
            if (!string.IsNullOrWhiteSpace(bio)) snippetParts.Add(bio);
            if (skillNames.Any()) snippetParts.Add(string.Join(", ", skillNames));
            if (expSummary.Any()) snippetParts.Add(string.Join("; ", expSummary));

            var snippet = string.Join(" | ", snippetParts);
            if (snippet.Length > 1000) snippet = snippet.Substring(0, 1000);

            results.Add(new SearchResultDto { SpecialistId = id, Score = score, Snippet = snippet });
        }

        // 4. Build RAG prompt
        var combined = string.Join("\n\n---\n\n", results.Select(r => $"[Score: {r.Score}] {r.Snippet}"));
        // Truncate context to token budget using tokenizer
        var truncated = _tokenizer.Truncate(combined, 2000);
        var prompt = PromptTemplate.Build(truncated, request.Query);

        // 5. Call chat (RAG)
        resp.Answer = await _chat.ChatAsync(prompt, cancellationToken);
        resp.Results = results;

        // 6. record SearchInteraction (best-effort)
        try
        {
            var si = new SearchInteraction
            {
                RawQuery = request.Query,
                ResultSpecialistIds = string.Join(',', results.Select(r => r.SpecialistId)),
                UserId = _currentUser.Id ?? Guid.Empty,
            };
            _db.Set<SearchInteraction>().Add(si);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // best-effort: log if logging is added; ignore otherwise
        }

        return resp;
    }
}
