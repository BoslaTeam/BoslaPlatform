using BoslaPlatform.Application.Features.Conversations.Requests;
using FluentValidation;

namespace BoslaPlatform.Application.Features.Conversations.Validators
{
    public sealed class SendMessageRequestValidator: AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.MessageText)
                .NotEmpty()
                .MaximumLength(4000);
        }
    }
}
