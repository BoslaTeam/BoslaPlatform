using System;

namespace BoslaPlatform.Application.Features.Admin.Requests
{
    public sealed class RescheduleAppointmentRequest
    {
        public DateTime NewStart { get; set; }
        public DateTime NewEnd { get; set; }
    }
}
