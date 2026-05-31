using BoslaPlatform.Domain.Models.Profile;
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

            builder.HasOne(s => s.VerifiedByUser).WithMany().HasForeignKey(s => s.VerifiedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(s => s.User)
                .WithOne(u => u.Specialist)
                .HasForeignKey<Specialist>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
