namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record UpdateProfileRequest(
        string Name,
        string Country,
        string Gender,
        string PreferredLanguage);
}
