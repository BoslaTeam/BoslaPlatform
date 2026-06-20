using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class AppointmentConfiguration : BaseEntityConfiguration<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(a => a.SessionTopic).HasMaxLength(500);
            builder.Property(a => a.Notes).HasMaxLength(2000);
            builder.Property(a => a.CancellationReason).HasMaxLength(1000);

            builder.HasOne(a => a.Specialist).WithMany(s => s.Appointments).HasForeignKey(a => a.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(
                nameof(Appointment.SpecialistId), nameof(Appointment.Start), nameof(Appointment.End))
                .IsUnique().HasFilter("[Status] != 'Cancelled'");

            builder.HasOne(x => x.Conversation)
                        .WithOne(x => x.Appointment)
                        .HasForeignKey<Conversation>(x => x.AppointmentId);

            builder.HasIndex(nameof(Appointment.SpecialistId), nameof(Appointment.Start));

            builder.HasOne(a => a.User)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

