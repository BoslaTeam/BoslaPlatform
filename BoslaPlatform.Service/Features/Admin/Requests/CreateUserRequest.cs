using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application.Features.Admin.Requests
{
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public int Role { get; set; }
    }
}
