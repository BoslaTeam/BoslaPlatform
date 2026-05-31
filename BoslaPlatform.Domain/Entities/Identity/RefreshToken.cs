using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;

namespace BoslaPlatform.Domain.Models.Identity
{
    public class RefreshToken: AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresOnUtc { get;  set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? CreatedByIp { get; set; }

        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
        public bool IsActive => !IsRevoked && DateTime.UtcNow <= ExpiresOnUtc;
        public User User { get; set; }



    }
}
