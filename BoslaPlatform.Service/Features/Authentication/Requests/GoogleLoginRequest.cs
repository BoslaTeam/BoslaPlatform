using System.ComponentModel.DataAnnotations;

namespace BoslaPlatform.Application
{
    public sealed class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID Token is required.")]
        public string IdToken { get; set; } = string.Empty;
    }
}
