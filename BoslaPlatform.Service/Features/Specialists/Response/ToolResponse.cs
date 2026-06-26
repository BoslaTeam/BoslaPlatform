using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed class ToolResponse
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
