using BoslaPlatform.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ReminderConfiguration : BaseEntityConfiguration<Reminder>
    {
        public override void Configure(EntityTypeBuilder<Reminder> builder)
        {
            base.Configure(builder);
            builder.Property(r => r.IsSent).HasDefaultValue(false);

            builder.HasOne(r => r.Appointment).WithMany(a => a.Reminders)
                .HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(r => r.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
