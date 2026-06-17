using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record SpecialistOnboardRequest(
    int ExperienceYears,
    ExperienceLevel ExperienceLevel,
    decimal HourlyRate,
    string? BookingPolicy
);
}
