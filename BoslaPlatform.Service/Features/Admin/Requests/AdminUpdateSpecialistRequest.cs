namespace BoslaPlatform.Application.Features.Admin.Requests;

public sealed class AdminUpdateSpecialistRequest
{
    // User fields
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public string? PreferredLanguage { get; set; }

    // Specialist fields
    public int? ExperienceYears { get; set; }
    public string? ExperienceLevel { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? BookingPolicy { get; set; }
    public string? VerificationStatus { get; set; }

    // Lookup relations
    public List<Guid>? ExpertiseIds { get; set; }
    public List<Guid>? IndustryIds { get; set; }
}
