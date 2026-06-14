using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class AddExperienceRequestDTO
    {
        public string JobTitle { get; init; } = string.Empty;

        public string CompanyName { get; init; } = string.Empty;

        public DateOnly FromDate { get; init; }

        public DateOnly? ToDate { get; init; }

        public string? Description { get; init; }
    }
}
