namespace BoslaPlatform.Application.Features.Admin.Requests
{
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? Gender { get; set; }
        public string? PreferredLanguage { get; set; }
        public bool IsActive { get; set; }
        public int Role { get; set; }
    }
}
