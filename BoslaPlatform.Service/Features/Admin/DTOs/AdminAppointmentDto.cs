using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public class AdminAppointmentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid SpecialistId { get; set; }
        public string SpecialistName { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Status { get; set; } // e.g. 0: Pending, 1: Confirmed, 2: Completed
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
