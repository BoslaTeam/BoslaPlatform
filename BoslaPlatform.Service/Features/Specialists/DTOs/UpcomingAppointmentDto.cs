using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class UpcomingAppointmentDto
    {
        public Guid AppointmentId { get; init; }

        public Guid ClientId { get; init; }

        public string ClientName { get; init; } = string.Empty;

        public string ServiceName { get; init; } = string.Empty;

        public DateTimeOffset StartTimeUtc { get; init; }

        public string Status { get; init; } = string.Empty;
    }
}
