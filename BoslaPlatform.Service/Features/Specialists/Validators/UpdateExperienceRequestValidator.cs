using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public class UpdateExperienceRequestValidator : AbstractValidator<UpdateExperienceRequest>
    {
        public UpdateExperienceRequestValidator()
        {
            RuleFor(x => x.CompanyName)
         .NotEmpty()
         .MaximumLength(200);

            RuleFor(x => x.JobTitle)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(2000);

            RuleFor(x => x.FromDate)
                .NotEmpty();

            RuleFor(x => x)
                .Must(x =>
                    x.ToDate is null ||
                    x.ToDate >= x.FromDate)
                .WithMessage(
                    "End date must be greater than or equal to start date.");
        }
    }
}
