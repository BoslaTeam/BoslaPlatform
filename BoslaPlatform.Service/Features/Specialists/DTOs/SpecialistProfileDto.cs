using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public sealed record SpecialistProfileDto(
    Guid SpecialistId,
    Guid UserId,
    string Email,
    string Name,
    string? Title,
    string? Bio,
    string? ProfileImageUrl,
    string? Country,
    string? Gender,
    string? PreferredLanguage,
    int ExperienceYears,
    ExperienceLevel ExperienceLevel,
    decimal HourlyRate,
    string? IntroVideoUrl,
    VerificationStatus VerificationStatus,
    string? BookingPolicy,
    int MinBookingNoticeHours,
    int MaxSessionsPerDay,
    int MaxSessionsPerWeek,
    int CancellationDeadlineHours,
    decimal CancellationFeePercent
);
}
