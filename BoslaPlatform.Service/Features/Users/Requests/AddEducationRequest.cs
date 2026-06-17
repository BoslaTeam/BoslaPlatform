namespace BoslaPlatform.Application.Features.Users.Requests
{
    public sealed record AddEducationRequest(
        string Degree,
        string Institution,
        int StartYear,
        int? EndYear);
}
