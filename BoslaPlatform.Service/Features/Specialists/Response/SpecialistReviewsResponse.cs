using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed class SpecialistReviewsResponse
    {
        public double AverageRating { get; init; }

        public int TotalReviews { get; init; }

        public PaginatedList<ReviewDto> Reviews { get; init; } = default!;
    }
}
