using BoslaPlatform.Domain.Enums;
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
            builder.Property(sr => sr.Url)
                .HasMaxLength(2000);

            builder.Property(sr => sr.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(RecordingStatus.Pending)
                .IsRequired();

            builder.Property(sr => sr.AccessControl).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(sr => sr.StorageProvider).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(sr => sr.AgoraRecordingId).HasMaxLength(512);
            builder.Property(sr => sr.AgoraRecordingSid).HasMaxLength(128);
            builder.HasOne(sr => sr.VideoSession)
                    .WithMany(v => v.Recordings)
                    .HasForeignKey(sr => sr.VideoSessionId)
                    .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(sr => sr.VideoSessionId);

        }
    }
}
