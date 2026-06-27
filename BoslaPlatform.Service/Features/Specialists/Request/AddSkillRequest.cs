using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class AddSkillRequest
    {
        public IReadOnlyList<Guid> SkillIds { get; init; } = [];
    }
}
