using System;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class RescheduleAppointmentRequest
    {
        public DateTimeOffset NewStart { get; set; }
        public DateTimeOffset NewEnd { get; set; }
        public string Reason { get; set; } = string.Empty;

        public RescheduleAppointmentRequest() { }

        public RescheduleAppointmentRequest(DateTimeOffset newStart, DateTimeOffset newEnd, string reason)
        {
            NewStart = newStart;
            NewEnd = newEnd;
            Reason = reason;
        }
    }
}