using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class AddReminderRequest
    {
        public DateTimeOffset ReminderTime { get; set; }
        public string Message { get; set; } = null!;
    }
}
