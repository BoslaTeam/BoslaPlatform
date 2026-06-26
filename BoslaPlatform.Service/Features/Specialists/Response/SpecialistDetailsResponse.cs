using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public class SpecialistDetailsResponse
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public string Email { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Title { get; init; }

        public string? Bio { get; init; }

        public string? ProfileImageUrl { get; init; }

        public string? Country { get; init; }

        public string Gender { get; init; }

        public string? PreferredLanguage { get; init; }

        public int ExperienceYears { get; init; }

        public ExperienceLevel ExperienceLevel { get; init; }

        public decimal HourlyRate { get; init; }

        public string? IntroVideoUrl { get; init; }

        public VerificationStatus VerificationStatus { get; init; }

        public List<ToolResponse> Tools { get; init; } = new();
        public List<string> Skills { get; init; } = new();
        public List<string> Industries { get; init; } = new();

        public decimal Rating { get; init; }

        public int ReviewsCount { get; init; }

        public bool IsOnline { get; init; }
    }
}
