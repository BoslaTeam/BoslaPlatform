using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddAvailabilitiesRequestValidator : AbstractValidator<AddAvailabilitiesRequest>
    {
        public AddAvailabilitiesRequestValidator()
        {
            RuleFor(x => x.Availabilities)
                .NotEmpty();

            RuleForEach(x => x.Availabilities)
                .SetValidator(new AvailabilityItemRequestValidator());
        }
    }
}
