using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application
{
    public sealed class ResendConfirmationEmailRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
