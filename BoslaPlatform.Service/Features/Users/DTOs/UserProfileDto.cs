namespace BoslaPlatform.Application.Features.Users.DTOs
{
    public sealed record UserProfileDto(
        Guid Id,
        string Email,
        string Name,
        string Country,
        string Gender,
        string PreferredLanguage,
        bool IsActive);
}
