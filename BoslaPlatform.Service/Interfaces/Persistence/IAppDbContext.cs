using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Conversations;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Lookup;
using BoslaPlatform.Domain.Models.Profile;
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

        DbSet<Appointment> Appointments { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Conversation> Conversations { get; }

        DbSet<Message> Messages { get; }

        DbSet<Notification> Notifications { get; }

        DbSet<Review> Reviews { get; }
        DbSet<TEntity> Set<TEntity>()
            where TEntity : class;

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
