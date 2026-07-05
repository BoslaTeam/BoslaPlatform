using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Appointments.Requests
{
    public sealed class AddReviewRequest
    {
        public byte Rating { get; init; }

        public string? Comment { get; init; }
    }
}
