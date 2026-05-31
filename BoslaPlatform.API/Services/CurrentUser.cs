using BoslaPlatform.Application.Interfaces;
using System.Security.Claims;

namespace BoslaPlatform.API.Services
{
    public class CurrentUser : IUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid? Id => Guid.TryParse(
            User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        public string? Role => User?.FindFirstValue(ClaimTypes.Role);

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
