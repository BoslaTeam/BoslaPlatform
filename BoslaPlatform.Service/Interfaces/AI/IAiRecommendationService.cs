using BoslaPlatform.Application.Features.Specialists.DTOs;

namespace BoslaPlatform.Application.Interfaces.AI;

public interface IAiRecommendationService
{
    Task<List<SpecialistListItemResponse>> GetRecommendationsAsync(int topK = 6, CancellationToken cancellationToken = default);
}
