namespace BoslaPlatform.Application.Features.Admin.DTOs;

public sealed class AdminSpecialistListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal HourlyRate { get; set; }
    public string ExperienceLevel { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ExpertiseAreas { get; set; } = [];
    public int TotalSessions { get; set; }
    public decimal TotalEarnings { get; set; }
    public bool IsEmbedded { get; set; }
}
