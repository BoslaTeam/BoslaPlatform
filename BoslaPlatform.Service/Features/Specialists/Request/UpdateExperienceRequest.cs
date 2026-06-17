using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public class UpdateExperienceRequest
    {
        public string CompanyName { get; init; } = string.Empty;

        public string JobTitle { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public DateOnly FromDate { get; init; }

        public DateOnly? ToDate { get; init; }
    }
}
