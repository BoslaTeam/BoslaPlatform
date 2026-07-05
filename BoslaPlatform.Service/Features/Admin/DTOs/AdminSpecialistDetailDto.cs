namespace BoslaPlatform.Application.Features.Admin.DTOs;

public sealed class AdminSpecialistDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal HourlyRate { get; set; }
    public string ExperienceLevel { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    public string? PreferredLanguage { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public double Rating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalSessions { get; set; }
    public decimal TotalEarnings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> ExpertiseAreas { get; set; } = [];
    public List<string> Industries { get; set; } = [];
    public List<SpecialistSkillItemDto> Skills { get; set; } = [];
    public List<SpecialistToolItemDto> Tools { get; set; } = [];
    public List<SpecialistExperienceItemDto> Experiences { get; set; } = [];
    public List<SpecialistReviewItemDto> Reviews { get; set; } = [];
    public List<SpecialistDocumentItemDto> Documents { get; set; } = [];
    public string? AdminNotes { get; set; }
}

public sealed class SpecialistDocumentItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
}

public sealed class SpecialistSkillItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SpecialistToolItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SpecialistExperienceItemDto
{
    public Guid Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Description { get; set; }
}

public sealed class SpecialistReviewItemDto
{
    public Guid Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
