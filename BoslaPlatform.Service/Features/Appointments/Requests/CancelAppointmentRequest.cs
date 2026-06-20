using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class CancelAppointmentRequest
    {
        public string Reason { get; set; } = string.Empty;

        public CancelAppointmentRequest() { }

        public CancelAppointmentRequest(string reason)
        {
            Reason = reason;
        }
    }
}