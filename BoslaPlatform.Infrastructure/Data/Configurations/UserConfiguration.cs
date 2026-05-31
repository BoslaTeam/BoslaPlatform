using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using BoslaPlatform.Domain.Models.Conversations;
using BoslaPlatform.Domain.Models.Identity;
using BoslaPlatform.Domain.Models.Junctions;
using BoslaPlatform.Domain.Models.Profile;
using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
            builder.Property(u => u.Title).HasMaxLength(150);
            builder.Property(u => u.Bio).HasMaxLength(2000);
            builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
            builder.Property(u => u.Country).HasMaxLength(100);
            builder.Property(u => u.Gender).HasMaxLength(20);
            builder.Property(u => u.PreferredLanguage).HasMaxLength(10);

            builder.HasIndex(u => u.LastLoginAt);
            builder.HasQueryFilter(u => u.IsActive);

            // Explicitly configure all relationships on the User entity to prevent duplicate foreign key generation (UserId / UserId1)
            // Note: The 1-to-1 relationship with Specialist is configured on the dependent side (Specialist) in SpecialistConfiguration.cs.

            // One-to-Many relationship with RefreshTokens
            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with Appointments
            builder.HasMany(u => u.Appointments)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many relationship with Notifications
            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with Reminders
            builder.HasMany(u => u.Reminders)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many relationship with Educations
            builder.HasMany(u => u.Educations)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with SocialLinks
            builder.HasMany(u => u.SocialLinks)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with ConversationParticipants
            builder.HasMany(u => u.ConversationParticipants)
                .WithOne(cp => cp.User)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many relationship with VideoSessionParticipants
            builder.HasMany(u => u.VideoSessionParticipants)
                .WithOne(vp => vp.User)
                .HasForeignKey(vp => vp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many relationship with UserExpertise (Junction Table)
            builder.HasMany(u => u.UserExpertise)
                .WithOne(ue => ue.User)
                .HasForeignKey(ue => ue.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with UserIndustries (Junction Table)
            builder.HasMany(u => u.UserIndustries)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
