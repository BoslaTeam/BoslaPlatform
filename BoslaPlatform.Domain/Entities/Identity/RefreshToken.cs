using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoslaPlatform.Domain.Models.Identity
{
    public class RefreshToken: AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresOnUtc { get;  set; }
        [NotMapped]
        public bool IsRevoked => RevokedAt != null; public DateTime? RevokedAt { get; set; }
        public string? CreatedByIp { get; set; }

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
        }
        [NotMapped]
        public bool IsActive => RevokedAt == null && ExpiresOnUtc > DateTime.UtcNow;

        [NotMapped]
        public bool IsExpired =>DateTime.UtcNow >= ExpiresOnUtc;
        public User User { get; set; }



    }
}
