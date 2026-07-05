using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public string[]? Roles { get; set; }
        public int Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
