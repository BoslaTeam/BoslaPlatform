using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application
{
    public sealed class RegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage =
                "Password must contain at least 8 characters, one uppercase letter, one lowercase letter, and one number.")]
        public string Password { get; set; } = string.Empty;
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(12)]
        public string PreferredLanguage { get; set; } = string.Empty;

        public string? Gender { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
