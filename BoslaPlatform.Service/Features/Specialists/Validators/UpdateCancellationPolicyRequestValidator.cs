using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    internal class UpdateCancellationPolicyRequestValidator : AbstractValidator<UpdateCancellationPolicyRequest>
    {
        public UpdateCancellationPolicyRequestValidator()
        {
            RuleFor(x => x.CancellationNoticeHours)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.CancellationPolicy)
                .MaximumLength(2000);
        }
    }
}
