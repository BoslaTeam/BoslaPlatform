using BoslaPlatform.Application.Features.Users.Requests;
using FluentValidation;

namespace BoslaPlatform.Service.Features.Users.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        }
    }

    public class AddEducationRequestValidator : AbstractValidator<AddEducationRequest>
    {
        public AddEducationRequestValidator()
        {
            RuleFor(x => x.Degree).NotEmpty();
            RuleFor(x => x.Institution).NotEmpty();
            RuleFor(x => x.StartYear).GreaterThan(1900);
            RuleFor(x => x.EndYear).GreaterThanOrEqualTo(x => x.StartYear).When(x => x.EndYear.HasValue);
        }
    }

    public class UpdateEducationRequestValidator : AbstractValidator<UpdateEducationRequest>
    {
        public UpdateEducationRequestValidator()
        {
            RuleFor(x => x.Degree).NotEmpty();
            RuleFor(x => x.Institution).NotEmpty();
            RuleFor(x => x.StartYear).GreaterThan(1900);
            RuleFor(x => x.EndYear).GreaterThanOrEqualTo(x => x.StartYear).When(x => x.EndYear.HasValue);
        }
    }

    public class AddSocialLinkRequestValidator : AbstractValidator<AddSocialLinkRequest>
    {
        public AddSocialLinkRequestValidator()
        {
            RuleFor(x => x.Platform).NotEmpty();
            RuleFor(x => x.Url).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Invalid URL format.");
        }
    }
}
