using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record UpdateSpecialistRequest(
        int ExperienceYears,
        ExperienceLevel ExperienceLevel,
        decimal HourlyRate,
        string? IntroVideoUrl,
        string? BookingPolicy
    );
}
