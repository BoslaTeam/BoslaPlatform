using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed class SpecialistAvailabilityResponse
    {
        public Guid Id { get; init; }

        public DateTimeOffset Start { get; init; }

        public DateTimeOffset End { get; init; }

        public bool IsBooked { get; init; }
    }
}
