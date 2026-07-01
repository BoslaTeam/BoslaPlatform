using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application.Features.Admin.Requests;

public sealed class CreateSpecialistRequest
{
    // User fields
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public string? PreferredLanguage { get; set; }

    // Specialist fields
    [Required, Range(0, 70)]
    public int ExperienceYears { get; set; }

    [Required]
    public string ExperienceLevel { get; set; } = "Mid";

    [Required, Range(0, 99999)]
    public decimal HourlyRate { get; set; }

    public string? BookingPolicy { get; set; }

    // Lookup relations
    public List<Guid> ExpertiseIds { get; set; } = [];
    public List<Guid> IndustryIds { get; set; } = [];
    public List<Guid> SkillIds { get; set; } = [];
    public List<Guid> ToolIds { get; set; } = [];
}
