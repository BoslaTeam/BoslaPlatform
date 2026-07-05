using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Portfolio.Requests
{
    public sealed record CreatePortfolioItemRequest(
        string Title,
        string? Description,
        string CoverImageUrl,
        List<string> ImageUrls,
        string? WorkUrl);
}
