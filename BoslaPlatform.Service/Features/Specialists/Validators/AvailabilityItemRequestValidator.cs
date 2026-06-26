using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AvailabilityItemRequestValidator : AbstractValidator<AvailabilityItemRequest>
    {
        public AvailabilityItemRequestValidator()
        {
            RuleFor(x => x.Start)
                .NotEmpty()
                .WithMessage("Start date is required.");

            RuleFor(x => x.End)
                .NotEmpty()
                .WithMessage("End date is required.");

            RuleFor(x => x)
                .Must(x => x.End > x.Start)
                .WithMessage("End time must be greater than start time.");

            RuleFor(x => x.Start)
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("Start must be in the future.");
        }
    }
}
