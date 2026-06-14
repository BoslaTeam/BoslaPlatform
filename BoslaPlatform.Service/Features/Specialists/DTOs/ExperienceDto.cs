using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class ExperienceDto
    {
        public Guid Id { get; init; }

        public string CompanyName { get; init; } = string.Empty;

        public string JobTitle { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DateOnly FromDate { get; init; }

        public DateOnly? ToDate { get; init; }
    }
}
