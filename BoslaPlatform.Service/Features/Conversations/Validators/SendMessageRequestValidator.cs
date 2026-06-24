using BoslaPlatform.Application.Features.Conversations.Requests;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Conversations.Validators
{
    public sealed class SendMessageRequestValidator: AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.MessageText)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
        }
    }
}
