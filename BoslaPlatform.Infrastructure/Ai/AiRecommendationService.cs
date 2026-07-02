using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI;

public class AiRecommendationService : IAiRecommendationService
{
    private readonly IAppDbContext _db;
    private readonly IEmbeddingService _emb;
    private readonly IVectorStore _vectors;
    private readonly IUser _currentUser;
    private readonly ILogger<AiRecommendationService> _logger;

    public AiRecommendationService(
        IAppDbContext db,
        IEmbeddingService emb,
        IVectorStore vectors,
        IUser currentUser,
        ILogger<AiRecommendationService> logger)
    {
        _db = db;
        _emb = emb;
        _vectors = vectors;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<List<SpecialistListItemResponse>> GetRecommendationsAsync(int topK = 6, CancellationToken cancellationToken = default)
    {
        var query = await BuildRecommendationQueryAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var qEmb = await _emb.CreateEmbeddingAsync(query, cancellationToken);
            var hits = await _vectors.SearchSimilarAsync(qEmb, topK * 2, cancellationToken);
            var ids = hits.Select(h => h.SpecialistId).ToList();

            var specialists = await _db.Specialists
                .Include(s => s.User)
                .Include(s => s.Reviews)
                .Include(s => s.Appointments)
                .Where(s => ids.Contains(s.Id))
                .ToListAsync(cancellationToken);

            if (specialists.Count > 0)
            {
                var scored = specialists
                    .Select(s => (
                        Specialist: s,
                        Score: hits.FirstOrDefault(h => h.SpecialistId == s.Id).Score
                    ))
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => MapToDto(x.Specialist))
                    .ToList();

                return scored;
            }
        }

        return await GetFallbackRecommendationsAsync(topK, cancellationToken);
    }

    private async Task<string?> BuildRecommendationQueryAsync(CancellationToken cancellationToken)
    {
        var uid = _currentUser.Id;
        if (uid == null || uid == Guid.Empty)
            return null;

        var parts = new List<string>();

        var recentSearches = await _db.Set<SearchInteraction>()
            .Where(si => si.UserId == uid.Value)
            .OrderByDescending(si => si.CreatedAtUtc)
            .Take(5)
            .Select(si => si.RawQuery)
            .ToListAsync(cancellationToken);

        parts.AddRange(recentSearches);

        var bookedExpertise = await _db.Appointments
            .Where(a => a.UserId == uid.Value && a.Status == AppointmentStatus.Completed)
            .SelectMany(a => a.Specialist.SpecialistExpertise)
            .Include(se => se.Expertise)
            .Select(se => se.Expertise.Name)
            .Distinct()
            .Take(3)
            .ToListAsync(cancellationToken);

        parts.AddRange(bookedExpertise.Select(e => $"خبير في {e}"));

        return parts.Count > 0 ? string.Join(" ", parts) : null;
    }

    private async Task<List<SpecialistListItemResponse>> GetFallbackRecommendationsAsync(int topK, CancellationToken cancellationToken)
    {
        var specialists = await _db.Specialists
            .Include(s => s.User)
            .Include(s => s.Reviews)
            .ToListAsync(cancellationToken);

        return specialists
            .OrderByDescending(s => s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : 0)
            .ThenByDescending(s => s.Reviews.Count)
            .Take(topK)
            .Select(MapToDto)
            .ToList();
    }

    private static SpecialistListItemResponse MapToDto(Specialist s)
    {
        return new SpecialistListItemResponse
        {
            Id = s.Id,
            Name = s.User.Name,
            Title = s.User.Title,
            ProfileImageUrl = s.User.ProfileImageUrl,
            HourlyRate = s.HourlyRate,
            ExperienceLevel = s.ExperienceLevel,
            VerificationStatus = s.Verification != null ? s.Verification.Status : VerificationStatus.Pending,
            Rating = s.Reviews.Any() ? Math.Round((decimal)s.Reviews.Average(r => r.Rating), 1) : 0,
            IsOnline = false,
        };
    }
}
