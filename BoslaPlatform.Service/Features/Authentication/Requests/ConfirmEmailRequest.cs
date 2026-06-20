using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application
{
    public sealed class ConfirmEmailRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = string.Empty;
    }
}
