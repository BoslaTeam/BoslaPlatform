using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public class SubmitReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
