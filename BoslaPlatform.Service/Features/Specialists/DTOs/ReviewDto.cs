using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class ReviewDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public string ReviewerName { get; init; } = string.Empty;

        public byte Rating { get; init; }

        public string? Comment { get; init; }

        public DateTimeOffset CreatedOnUtc { get; init; }
    }
}
