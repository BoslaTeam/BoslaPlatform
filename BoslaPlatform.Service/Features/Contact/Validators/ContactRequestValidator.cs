using FluentValidation;
using BoslaPlatform.Application.Features.Contact.Requests;

namespace BoslaPlatform.Application.Features.Contact.Validators
{
    public class ContactRequestValidator : AbstractValidator<ContactRequest>
    {
        private static readonly string[] ValidSubjects =
            ["general", "support", "partnership", "specialist", "complaint", "other"];

        public ContactRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode("Contact.NameRequired")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.")
                .WithErrorCode("Contact.NameTooLong");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .WithErrorCode("Contact.EmailRequired")
                .EmailAddress()
                .WithMessage("A valid email address is required.")
                .WithErrorCode("Contact.InvalidEmail");

            RuleFor(x => x.Subject)
                .NotEmpty()
                .WithMessage("Subject is required.")
                .WithErrorCode("Contact.SubjectRequired")
                .Must(v => ValidSubjects.Contains(v))
                .WithMessage($"Subject must be one of: {string.Join(", ", ValidSubjects)}.")
                .WithErrorCode("Contact.InvalidSubject");

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message is required.")
                .WithErrorCode("Contact.MessageRequired")
                .MaximumLength(5000)
                .WithMessage("Message cannot exceed 5000 characters.")
                .WithErrorCode("Contact.MessageTooLong");
        }
    }
}
