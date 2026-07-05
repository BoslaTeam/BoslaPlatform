using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.DTOs
{
    public class ReminderDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public DateTimeOffset ReminderTime { get; set; }
        public string Message { get; set; } = null!;
        public bool IsSent { get; set; }
    }
}
