using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddToolsRequestValidator : AbstractValidator<AddToolRequest>
    {
        public AddToolsRequestValidator()
        {
            RuleFor(x => x.ToolIds)
                .NotEmpty();

            RuleForEach(x => x.ToolIds)
                .NotEmpty();
        }
    }
}
