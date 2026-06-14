using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public class UpdateBookingPolicyRequest
    {
        public string BookingPolicy { get; init; } = string.Empty;
        public int MinBookingNoticeHours { get; init; }

        public int MaxSessionsPerDay { get; init; }

        public int MaxSessionsPerWeek { get; init; }
    }
}

