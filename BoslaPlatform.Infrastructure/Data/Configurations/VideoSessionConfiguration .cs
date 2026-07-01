using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class VideoSessionConfiguration : BaseEntityConfiguration<VideoSession>
    {
        public override void Configure(EntityTypeBuilder<VideoSession> builder)
        {
            base.Configure(builder);
            builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(v => v.AgoraChannelName).HasMaxLength(100).IsRequired();
            builder.Property(v => v.AgoraAppId).HasMaxLength(100).IsRequired();
            builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(v => v.AgoraRecordingId).HasMaxLength(200);
            builder.Property(v => v.AgoraRecordingSid).HasMaxLength(200);
            builder.Property(v => v.RecordingUrl)
                .HasMaxLength(2000);

            builder.HasOne(v => v.Appointment)
                .WithOne(a => a.VideoSession)
                .HasForeignKey<VideoSession>(v => v.AppointmentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(v => v.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(VideoSessionStatus.Waiting)
                .IsRequired();

            builder.Metadata
                .FindNavigation(nameof(VideoSession.Participants))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(v => v.AgoraChannelName).IsUnique();
            builder.HasIndex(v => v.AppointmentId)
                .IsUnique();
        }
    }
}
