using System;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class CreateAppointmentRequest
    {
        public Guid SpecialistId { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string? SessionTopic { get; set; }
        public string? Notes { get; set; }
    }
}