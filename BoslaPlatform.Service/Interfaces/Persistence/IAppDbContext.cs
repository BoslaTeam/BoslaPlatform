using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Interfaces.Persistence
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        DbSet<Specialist> Specialists { get; }
        DbSet<SpecialistExperience> SpecialistExperiences { get; }

        DbSet<Education> Educations { get; }

        DbSet<SocialLink> SocialLinks { get; }

        DbSet<Availability> AvailabilitySlots { get; }

        DbSet<Skill> Skills { get; }

        DbSet<Industry> Industries { get; }

        DbSet<Expertise> Expertises { get; }

        DbSet<Tool> Tools { get; }

        DbSet<SpecialistExpertise> SpecialistExpertise { get; }

        DbSet<Appointment> Appointments { get; }

        DbSet<Payment> Payments { get; }
        DbSet<BoslaPlatform.Domain.Models.Communication.Conversation> Conversations { get; }
        DbSet<ConversationParticipant> ConversationParticipants { get; }
        DbSet<Message> Messages { get; }

        DbSet<Notification> Notifications { get; }

        DbSet<SpecialistSkill> SpecialistSkills { get; }
        DbSet<Review> Reviews { get; }
        DbSet<VideoSession> VideoSessions { get; }
        DbSet<VideoSessionParticipant> VideoSessionParticipants { get; }
        DbSet<SpecialistTool> SpecialistTools { get; }

        DbSet<TEntity> Set<TEntity>()
            where TEntity : class;

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
