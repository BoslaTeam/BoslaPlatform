using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Shared.Dtos.Auth
{
    public class RegisterRequest
    {
        [Required]
        public string Name { get; set; }
        [EmailAddress]
        [Required(ErrorMessage = "Email is required!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required!")]
        [RegularExpression( @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must contain at least 8 characters, one uppercase letter, one lowercase letter, and one number.")] 
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
