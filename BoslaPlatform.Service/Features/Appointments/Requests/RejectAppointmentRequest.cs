using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class RejectAppointmentRequest
    {
        public string Reason { get; set; } = string.Empty;

        public RejectAppointmentRequest() { }

        public RejectAppointmentRequest(string reason)
        {
            Reason = reason;
        }
    }
}