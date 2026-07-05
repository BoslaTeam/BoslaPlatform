using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Portfolio.Requests
{
    public sealed record UpdatePortfolioItemRequest(
        string Title,
        string? Description,
        string CoverImageUrl,
        List<string> ImageUrls,
        string? WorkUrl);
}
