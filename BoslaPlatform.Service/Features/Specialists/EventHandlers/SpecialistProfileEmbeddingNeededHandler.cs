using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Specialists;
using BoslaPlatform.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistProfileEmbeddingNeededHandler : INotificationHandler<SpecialistProfileEmbeddingNeededEvent>
    {
        private const string EmbeddingModel = "models/gemini-embedding-001";

        private readonly IAppDbContext _db;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly ILogger<SpecialistProfileEmbeddingNeededHandler> _logger;

        public SpecialistProfileEmbeddingNeededHandler(
            IAppDbContext db,
            IEmbeddingService embeddingService,
            IVectorStore vectorStore,
            ILogger<SpecialistProfileEmbeddingNeededHandler> logger)
        {
            _db = db;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _logger = logger;
        }

        public async Task Handle(SpecialistProfileEmbeddingNeededEvent notification, CancellationToken cancellationToken)
        {
            var specialist = await _db.Specialists
                .Include(s => s.User)
                .Include(s => s.SpecialistSkills!).ThenInclude(ss => ss.Skill)
                .Include(s => s.SpecialistTools!).ThenInclude(st => st.Tool)
                .Include(s => s.SpecialistExpertise!).ThenInclude(se => se.Expertise)
                .Include(s => s.Experiences)
                .FirstOrDefaultAsync(s => s.Id == notification.SpecialistId, cancellationToken);

            if (specialist == null)
            {
                _logger.LogWarning("Specialist {Id} not found for embedding", notification.SpecialistId);
                return;
            }

            var content = BuildContent(specialist);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("No content to embed for specialist {Id}", notification.SpecialistId);
                return;
            }

            var contentHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(content)));

            var embeddingJson = await _embeddingService.CreateEmbeddingAsync(content, cancellationToken);
            if (string.IsNullOrWhiteSpace(embeddingJson) || embeddingJson == "[]")
            {
                _logger.LogError("Failed to create embedding for specialist {Id}", notification.SpecialistId);
                return;
            }

            await _vectorStore.StoreEmbeddingAsync(
                specialist.Id, embeddingJson, EmbeddingModel, contentHash, cancellationToken);

            var existing = await _db.Set<SpecialistEmbedding>()
                .FirstOrDefaultAsync(e => e.SpecialistId == specialist.Id, cancellationToken);

            if (existing != null)
            {
                existing.EmbeddingVector = embeddingJson;
                existing.EmbeddingModel = EmbeddingModel;
                existing.ContentHash = contentHash;
                existing.LastEmbeddedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                _db.Set<SpecialistEmbedding>().Add(new SpecialistEmbedding
                {
                    SpecialistId = specialist.Id,
                    EmbeddingVector = embeddingJson,
                    EmbeddingModel = EmbeddingModel,
                    ContentHash = contentHash,
                    LastEmbeddedAt = DateTimeOffset.UtcNow,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Embedding updated for specialist {Id}", notification.SpecialistId);
        }

        private static string BuildContent(Domain.Entities.Profile.Specialist specialist)
        {
            var parts = new List<string>();

            var user = specialist.User;
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(user.Title)) parts.Add(user.Title);
                if (!string.IsNullOrWhiteSpace(user.Bio)) parts.Add(user.Bio);
            }

            var skillNames = specialist.SpecialistSkills?
                .Where(ss => ss.Skill != null && !string.IsNullOrWhiteSpace(ss.Skill.Name))
                .Select(ss => ss.Skill.Name) ?? [];
            if (skillNames.Any()) parts.Add($"المهارات: {string.Join("، ", skillNames)}");

            var toolNames = specialist.SpecialistTools?
                .Where(st => st.Tool != null && !string.IsNullOrWhiteSpace(st.Tool.Name))
                .Select(st => st.Tool.Name) ?? [];
            if (toolNames.Any()) parts.Add($"الأدوات: {string.Join("، ", toolNames)}");

            var expertiseNames = specialist.SpecialistExpertise?
                .Where(se => se.Expertise != null && !string.IsNullOrWhiteSpace(se.Expertise.Name))
                .Select(se => se.Expertise.Name) ?? [];
            if (expertiseNames.Any()) parts.Add($"التخصصات: {string.Join("، ", expertiseNames)}");

            var expSummaries = specialist.Experiences?
                .Select(e => $"{e.JobTitle} في {e.CompanyName}") ?? [];
            if (expSummaries.Any()) parts.Add($"الخبرات: {string.Join("؛ ", expSummaries)}");

            return string.Join(" | ", parts);
        }
    }
}
