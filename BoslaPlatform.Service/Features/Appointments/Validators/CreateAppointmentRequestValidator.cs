using System;
using FluentValidation;
using BoslaPlatform.Application.Features.Appointments.Requests;

namespace BoslaPlatform.Application.Features.Appointments.Validators
{
    public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
    {
        public CreateAppointmentRequestValidator()
        {
            // 1. Validate SpecialistId
            RuleFor(x => x.SpecialistId)
                .NotEmpty()
                .WithMessage("Specialist identifier is required.")
                .WithErrorCode("Appointment.SpecialistIdRequired");

            // 2. Validate Start Time
            RuleFor(x => x.Start)
                .NotEmpty()
                .WithMessage("Appointment start date and time are required.")
                .WithErrorCode("Appointment.StartRequired")
                .Must(BeInTheFuture)
                .WithMessage("The appointment start time must be in the future.")
                .WithErrorCode("Appointment.StartInPast");

            // 3. Validate End Time
            RuleFor(x => x.End)
                .NotEmpty()
                .WithMessage("Appointment end date and time are required.")
                .WithErrorCode("Appointment.EndRequired")
                .GreaterThan(x => x.Start)
                .WithMessage("The appointment end time must be strictly after the start time.")
                .WithErrorCode("Appointment.InvalidTimeRange");

            // 4. Validate Session Topic (Optional but has a maximum length constraint)
            RuleFor(x => x.SessionTopic)
                .MaximumLength(200)
                .WithMessage("The session topic cannot exceed 200 characters.")
                .WithErrorCode("Appointment.TopicTooLong");

            // 5. Validate Notes (Optional but has a maximum length constraint)
            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters.")
                .WithErrorCode("Appointment.NotesTooLong");
        }

        /// <summary>
        /// Custom validator method to ensure the DateTimeOffset is set in the future.
        /// Compares the input to the current global UTC time to maintain system consistency.
        /// </summary>
        private bool BeInTheFuture(DateTimeOffset startDateTime)
        {
            // Enforcing the Bosla Platform rule: All datetimes are handled in UTC
            return startDateTime > DateTimeOffset.UtcNow;
        }
    }
}