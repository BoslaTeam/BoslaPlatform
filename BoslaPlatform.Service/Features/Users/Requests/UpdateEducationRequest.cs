namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record UpdateEducationRequest(
        string Degree,
        string Institution,
        int StartYear,
        int? EndYear);
}
