using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddExpertiseRequestValidator : AbstractValidator<AddExpertiseRequest>
    {
        public AddExpertiseRequestValidator()
        {
            RuleFor(x => x.ExpertiseId)
                .NotEmpty();
        }
    }
}
