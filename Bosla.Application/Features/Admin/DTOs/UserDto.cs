using System;

namespace Bosla.Application.Features.Admin.DTOs
{
    public sealed class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public string[]? Roles { get; set; }
    }
}
