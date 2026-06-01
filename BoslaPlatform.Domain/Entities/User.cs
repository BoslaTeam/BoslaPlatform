using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Conversations;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.Models.Video;
using Microsoft.AspNetCore.Identity;

namespace BoslaPlatform.Domain.Entities
{
    public class User : IdentityUser<Guid>,IAuditableEntity
    {
        public string Name { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Country { get; set; }
        public string? Gender { get; set; }
        public string? PreferredLanguage { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Specialist? Specialist { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public ICollection<Appointment> Appointments { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
        public ICollection<Reminder> Reminders { get; set; } = [];
        public ICollection<Education> Educations { get; set; } = [];
        public ICollection<SocialLink> SocialLinks { get; set; } = [];
        public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = [];
        public ICollection<VideoSessionParticipant> VideoSessionParticipants { get; set; } = [];
        public ICollection<UserExpertise> UserExpertise { get; set; } = [];
        public ICollection<UserIndustry> UserIndustries { get; set; } = [];
        public ICollection<Specialist> VerifiedSpecialists { get; set; } = new List<Specialist>();
        public DateTimeOffset CreatedAtUtc { get ; set ; }
        public Guid? CreatedBy { get ; set ; }
        public DateTimeOffset? LastModifiedUtc { get ; set ; }
        public Guid? LastModifiedBy { get ; set ; }
    }

}
