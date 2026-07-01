using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class SpecialistDetailsDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public decimal HourlyRate { get; set; }
        public int ExperienceYears { get; set; }
        public string? VerificationStatus { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
