using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class AddToolRequest
    {
        public IReadOnlyList<Guid> ToolIds { get; init; } = [];
    }
}
