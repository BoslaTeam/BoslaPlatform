namespace BoslaPlatform.Application.Features.Users.DTOs
{
    public sealed record EducationDto(
        Guid Id,
        string Degree,
        string Institution,
        int StartYear,
        int? EndYear);
}
