using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid SpecialistId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Status { get; set; }
        public decimal Price { get; set; }
    }
}
