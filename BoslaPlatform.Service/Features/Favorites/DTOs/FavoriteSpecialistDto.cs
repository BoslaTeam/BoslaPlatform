using System;

namespace BoslaPlatform.Application.Features.Favorites.DTOs
{
    public sealed record FavoriteSpecialistDto(
        Guid Id,
        Guid SpecialistId,
        string Name,
        string? Title,
        string? ImageUrl,
        double Rating,
        bool IsVerified,
        int ExperienceLevel,
        decimal HourlyRate,
        DateTimeOffset CreatedAtUtc);
}
