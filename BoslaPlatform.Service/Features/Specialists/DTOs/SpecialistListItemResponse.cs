using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class SpecialistListItemResponse
    {

        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Title { get; init; }

        public string? ProfileImageUrl { get; init; }

        public decimal HourlyRate { get; init; }

        public ExperienceLevel ExperienceLevel { get; init; }

        public VerificationStatus VerificationStatus { get; init; }

        public decimal Rating { get; init; }

        public bool IsOnline { get; init; }
    }
}
