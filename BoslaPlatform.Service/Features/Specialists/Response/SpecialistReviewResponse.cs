using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public class SpecialistReviewResponse
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public string UserName { get; init; } = string.Empty;

        public int Rating { get; init; }

        public string? Comment { get; init; }

        public DateTimeOffset CreatedOnUtc { get; init; }
    }
}
