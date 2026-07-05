using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddSkillsRequestValidator : AbstractValidator<AddSkillRequest>
    {
        public AddSkillsRequestValidator()
        {
            RuleFor(x => x.SkillIds)
                .NotEmpty();

            RuleForEach(x => x.SkillIds)
                .NotEmpty();
        }
    }
}
