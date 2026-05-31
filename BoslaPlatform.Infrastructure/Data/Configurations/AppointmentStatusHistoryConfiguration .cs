using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class AppointmentStatusHistoryConfiguration : BaseEntityConfiguration<AppointmentStatusHistory>
    {
        public override void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
        {
            base.Configure(builder);
            builder.Property(h => h.OldStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(h => h.Reason).HasMaxLength(500);

            builder.HasOne(h => h.Appointment).WithMany(a => a.StatusHistory)
                .HasForeignKey(h => h.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(h => h.ChangedByUser).WithMany().HasForeignKey(h => h.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
