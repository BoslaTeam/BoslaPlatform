using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddAvailabilityRequestValidator : AbstractValidator<AddAvailabilityRequest>
    {
        public AddAvailabilityRequestValidator()
        {
            RuleFor(x => x.Day)
                .NotEmpty().WithMessage("Day is required.")
                .Must(BeAValidDay).WithMessage("Invalid day name. Please enter a valid day of the week (e.g., Monday).");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$")
                .WithMessage("Start time must be in HH:mm format (e.g., 09:00).");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$")
                .WithMessage("End time must be in HH:mm format (e.g., 12:00).");

            RuleFor(x => x)
                .Must(x => BeAfterStartTime(x.StartTime, x.EndTime))
                .WithMessage("End time must be greater than start time.")
                .When(x => !string.IsNullOrEmpty(x.StartTime) && !string.IsNullOrEmpty(x.EndTime));
        }

        private bool BeAValidDay(string day)
        {
            return Enum.TryParse<DayOfWeek>(day, true, out _);
        }

        private bool BeAfterStartTime(string startTime, string endTime)
        {
            if (TimeSpan.TryParse(startTime, out var start) && TimeSpan.TryParse(endTime, out var end))
            {
                return end > start;
            }
            return false;
        }
    }
}