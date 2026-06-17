using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddAvailabilityRequestValidator : AbstractValidator<AddAvailabilityRequest>
    {
        public AddAvailabilityRequestValidator()
        {
            RuleFor(x => x.Start)
                .NotEmpty();

            RuleFor(x => x.End)
                .NotEmpty();

            RuleFor(x => x)
                .Must(x => x.End > x.Start)
                .WithMessage("End time must be greater than start time.");
        }
    }
}
