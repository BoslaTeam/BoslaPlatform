using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class ReviewDto
    {
        public string ReviewerName { get; init; } = string.Empty;

        public byte Rating { get; init; }

        public string? Comment { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
