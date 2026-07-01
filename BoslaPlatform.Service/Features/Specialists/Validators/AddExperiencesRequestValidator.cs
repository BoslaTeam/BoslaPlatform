using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddExperiencesRequestValidator : AbstractValidator<AddExperiencesRequest>
    {
        public AddExperiencesRequestValidator()
        {
            RuleFor(x => x.Experiences)
                .NotEmpty();

            RuleForEach(x => x.Experiences)
                .SetValidator(new AddExperienceRequestValidator());
        }
    }
}
