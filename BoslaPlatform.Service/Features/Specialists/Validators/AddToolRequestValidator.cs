using BoslaPlatform.Application.Features.Specialists.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Validators
{
    public sealed class AddToolRequestValidator: AbstractValidator<AddToolRequest>
    {
        public AddToolRequestValidator()
        {
            RuleFor(x => x.ToolId).NotEmpty();
        }
    }
}
