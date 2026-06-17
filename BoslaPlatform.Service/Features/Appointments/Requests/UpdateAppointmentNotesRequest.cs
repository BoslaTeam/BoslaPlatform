using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class UpdateAppointmentNotesRequest
    {
        public string Notes { get; set; } = string.Empty;

        public UpdateAppointmentNotesRequest() { }

        public UpdateAppointmentNotesRequest(string notes)
        {
            Notes = notes;
        }
    }
}