using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;

namespace BoslaPlatform.Application.Interfaces.AI;

public interface IAiSearchService
{
    Task<AiSearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<List<SearchHistoryItemDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task RecordFeedbackAsync(Guid searchInteractionId, FeedbackRequest request, CancellationToken cancellationToken = default);
}
