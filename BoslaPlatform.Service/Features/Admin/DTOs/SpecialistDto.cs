using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class SpecialistDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public decimal HourlyRate { get; set; }
        public string? VerificationStatus { get; set; }
    }
}
