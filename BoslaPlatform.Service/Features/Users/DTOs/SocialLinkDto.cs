namespace BoslaPlatform.Application.Features.Users.DTOs
{
    public sealed record SocialLinkDto(
        Guid Id,
        string Platform,
        string Url);
}
