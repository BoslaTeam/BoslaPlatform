using BoslaPlatform.Application.Features.Specialists.Request;
using BoslaPlatform.Domain.Enums;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public class SpecialistOnboardRequestValidator
    : AbstractValidator<SpecialistOnboardRequest>
    {
        public SpecialistOnboardRequestValidator()
        {
            RuleFor(x => x.ExperienceYears)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(60);

            RuleFor(x => x.HourlyRate)
                .GreaterThan(0);

            RuleFor(x => x.BookingPolicy)
                .MaximumLength(2000);

            RuleFor(x => x)
                .Must(BeConsistentExperienceLevel)
                .WithMessage("Experience level does not match experience years.");
        }

        private static bool BeConsistentExperienceLevel(
            SpecialistOnboardRequest request)
        {
            return request.ExperienceLevel switch
            {
                ExperienceLevel.Entry => request.ExperienceYears <= 2,
                ExperienceLevel.Mid => request.ExperienceYears is >= 3 and <= 5,
                ExperienceLevel.Senior => request.ExperienceYears is >= 6 and <= 10,
                ExperienceLevel.Lead => request.ExperienceYears >= 10,
                _ => false
            };
        }
    }
}
