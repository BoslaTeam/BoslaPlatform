using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed class SpecialistAvailabilityResponse
    {
        public Guid Id { get; init; }

        public DayOfWeek DayOfWeek { get; init; }

        public TimeOnly StartTime { get; init; }

        public TimeOnly EndTime { get; init; }
    }
}
