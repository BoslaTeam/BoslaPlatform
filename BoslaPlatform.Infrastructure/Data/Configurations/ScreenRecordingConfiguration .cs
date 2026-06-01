using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ScreenRecordingConfiguration : BaseEntityConfiguration<ScreenRecording>
    {
        public override void Configure(EntityTypeBuilder<ScreenRecording> builder)
        {
            base.Configure(builder);
            builder.Property(sr => sr.Url).HasMaxLength(500).IsRequired();
            builder.Property(sr => sr.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(sr => sr.AccessControl).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(sr => sr.StorageProvider).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(sr => sr.AgoraRecordingId).HasMaxLength(200);
            builder.Property(sr => sr.AgoraRecordingSid).HasMaxLength(200);

            builder.HasOne(sr => sr.Appointment).WithOne(a => a.ScreenRecording)
                .HasForeignKey<ScreenRecording>(sr => sr.AppointmentId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(sr => sr.AppointmentId).IsUnique();
        }
    }
}
