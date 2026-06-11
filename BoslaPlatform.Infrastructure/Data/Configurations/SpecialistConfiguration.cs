using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SpecialistConfiguration : BaseEntityConfiguration<Specialist>
    {
        public override void Configure(EntityTypeBuilder<Specialist> builder)
        {
            base.Configure(builder);
            builder.Property(s => s.ExperienceLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(s => s.HourlyRate).HasPrecision(10, 2).IsRequired();
            builder.Property(s => s.MinBookingNoticeHours).HasDefaultValue(24);
            builder.Property(s => s.MaxSessionsPerDay).HasDefaultValue(8);
            builder.Property(s => s.MaxSessionsPerWeek).HasDefaultValue(40);
            builder.Property(s => s.IntroVideoUrl).HasMaxLength(500);
            builder.Property(s => s.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(s => s.CancellationDeadlineHours).HasDefaultValue(24);
            builder.Property(s => s.CancellationFeePercent).HasPrecision(5, 2).HasDefaultValue(0.00m);
            builder.Property(s => s.BookingPolicy).HasMaxLength(2000);

            // Explicitly configure all relationships on the Specialist entity to prevent duplicate foreign key generation (SpecialistId / SpecialistId1)

            // User Relationship (1-to-1)
            builder.HasOne(s => s.User)
                .WithOne(u => u.Specialist)
                .HasForeignKey<Specialist>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.UserId)
                .IsUnique();

            // VerifiedByUser Relationship (Many-to-One, no inverse navigation collection on User)
            builder.HasOne(s => s.VerifiedByUser)
                    .WithMany(u => u.VerifiedSpecialists)
                    .HasForeignKey(s => s.VerifiedBy)
                    .OnDelete(DeleteBehavior.NoAction);

            // Note: The 1-to-1 relationship with SpecialistEmbedding is configured on the dependent side (SpecialistEmbedding) in SpecialistEmbeddingConfiguration.cs.

            // Collections
            builder.HasMany(s => s.Appointments)
                .WithOne(a => a.Specialist)
                .HasForeignKey(a => a.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Availabilities)
                .WithOne(av => av.Specialist)
                .HasForeignKey(av => av.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Reviews)
                .WithOne(r => r.Specialist)
                .HasForeignKey(r => r.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Experiences)
                .WithOne(e => e.Specialist)
                .HasForeignKey(e => e.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SpecialistExpertise)
                .WithOne(se => se.Specialist)
                .HasForeignKey(se => se.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SpecialistIndustries)
                .WithOne(si => si.Specialist)
                .HasForeignKey(si => si.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SpecialistSkills)
                .WithOne(ss => ss.Specialist)
                .HasForeignKey(ss => ss.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.SpecialistTools)
                .WithOne(st => st.Specialist)
                .HasForeignKey(st => st.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
