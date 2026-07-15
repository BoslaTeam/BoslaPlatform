using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record UpdateSpecialistRequest(
        int ExperienceYears,
        ExperienceLevel ExperienceLevel,
        decimal HourlyRate,
        string? IntroVideoUrl,
        string? BookingPolicy,
        string? Title,
        string? Bio,
        string? Gender,
        string? PreferredLanguage,
        string? Country
    );
}
