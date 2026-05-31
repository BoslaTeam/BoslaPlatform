using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ReviewConfiguration : BaseEntityConfiguration<Review>
    {
        public override void Configure(EntityTypeBuilder<Review> builder)
        {
            base.Configure(builder);
            builder.Property(r => r.Comment).HasMaxLength(2000);
            builder.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");

            builder.HasOne(r => r.Appointment).WithOne(a => a.Review).HasForeignKey<Review>(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Specialist).WithMany(s => s.Reviews).HasForeignKey(r => r.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Reviewer).WithMany().HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.AppointmentId).IsUnique();
        }
    }
}
