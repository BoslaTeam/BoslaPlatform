using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Admin.Requests
{
    public sealed class UpdateUserRolesRequest
    {
        public List<string> Roles { get; set; } = new();
    }
}
