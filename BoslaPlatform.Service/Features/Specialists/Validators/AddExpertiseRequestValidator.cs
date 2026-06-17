using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddExpertiseRequestValidator : AbstractValidator<AddExperienceRequestDTO>
    {
        public AddExpertiseRequestValidator()
        {
            //RuleFor(x => x.ExpertiseId)
            //    .NotEmpty();

            RuleFor(x => x.JobTitle)
            .NotEmpty()
            .MaximumLength(200);

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.FromDate)
                .NotEmpty();

            RuleFor(x => x)
                .Must(x =>
                    !x.ToDate.HasValue ||
                    x.ToDate >= x.FromDate)
                .WithMessage("ToDate must be greater than FromDate.");
        }



    }
}
