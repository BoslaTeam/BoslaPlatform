namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record UpdateProfileRequest(
        string? Name,
        string? Title,
        string? Bio,
        string? ProfileImageUrl,
        string? PhoneNumber,
        string? Country,
        string? Gender,
        string? PreferredLanguage);
}
