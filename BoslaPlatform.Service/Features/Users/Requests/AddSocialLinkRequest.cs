namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record AddSocialLinkRequest(
        string Platform,
        string Url);
}
