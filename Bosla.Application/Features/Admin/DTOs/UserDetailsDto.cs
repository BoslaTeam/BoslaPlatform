using System;
using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public string[]? Roles { get; set; }
        public int Role { get; set; }
        public DateTime CreatedAt { get; set; }

        // Profile fields
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? Gender { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Related data
        public List<EducationItemDto> Education { get; set; } = new();
        public List<SocialLinkItemDto> SocialLinks { get; set; } = new();
        public int AppointmentsCount { get; set; }
    }

    public sealed class EducationItemDto
    {
        public Guid Id { get; set; }
        public string Degree { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
    }

    public sealed class SocialLinkItemDto
    {
        public Guid Id { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
