using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class UpdateBookingPolicyRequestValidator : AbstractValidator<UpdateBookingPolicyRequest>
    {
        public UpdateBookingPolicyRequestValidator()
        {
            RuleFor(x => x.BookingPolicy)
                .NotEmpty()
                .MaximumLength(2000);

            RuleFor(x => x.MinBookingNoticeHours)
                .InclusiveBetween(1, 168);

            RuleFor(x => x.MaxSessionsPerDay)
                .GreaterThan(0);

            RuleFor(x => x.MaxSessionsPerWeek)
                .GreaterThan(0);
        }
    }
}
