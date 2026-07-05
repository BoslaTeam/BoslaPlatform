using System;
using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Portfolio.DTOs
{
    public sealed record PortfolioItemDto(
        Guid Id,
        string Title,
        string? Description,
        string CoverImageUrl,
        string? WorkUrl,
        string Status,
        string? AdminNotes,
        int SortOrder,
        DateTimeOffset CreatedAtUtc,
        List<PortfolioItemImageDto> Images);

    public sealed record PortfolioItemImageDto(
        Guid Id,
        string ImageUrl,
        int SortOrder);
}
