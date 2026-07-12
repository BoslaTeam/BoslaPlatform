using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using static BoslaPlatform.Domain.Enums.UploadStatus;
using static BoslaPlatform.Domain.Enums.StorageProvider;
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

            builder.Property(v => v.RecordingStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired(false);

            builder.Property(v => v.RecordingStartedAtUtc)
                .IsRequired(false);

            builder.Property(v => v.RecordingFailureReason)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(v => v.CurrentRecordingId)
                .IsRequired(false);

            builder.HasOne(v => v.CurrentRecording)
                .WithMany()
                .HasForeignKey(v => v.CurrentRecordingId)
                .OnDelete(DeleteBehavior.SetNull);

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

            builder.Metadata
                .FindNavigation(nameof(VideoSession.Recordings))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Property(v => v.UploadStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(UploadStatus.Pending)
                .IsRequired();

            builder.Property(v => v.StorageProvider)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired(false);

            builder.Property(v => v.BucketName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(v => v.ObjectKey)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(v => v.ContentType)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(v => v.ContentLength)
                .IsRequired(false);

            builder.Property(v => v.UploadAttempts)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(v => v.LastUploadError)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(v => v.UploadedAtUtc)
                .IsRequired(false);

            builder.Property(v => v.ChecksumSha256)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(v => v.VersionId)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(v => v.ETag)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(v => v.LastUploadAttemptUtc)
                .IsRequired(false);

            builder.HasIndex(v => v.AgoraChannelName).IsUnique();
            builder.HasIndex(v => v.AppointmentId)
                .IsUnique();

            // Reconciliation query: efficiently find Pending/Retrying sessions due for retry
            builder.HasIndex(v => new { v.UploadStatus, v.NextRetryAtUtc })
                .HasDatabaseName("IX_VideoSessions_UploadStatus_NextRetryAtUtc");

            // Retention / soft-delete new columns
            builder.Property(v => v.RetryCount)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(v => v.LastRetryAtUtc)
                .IsRequired(false);

            builder.Property(v => v.NextRetryAtUtc)
                .IsRequired(false);

            builder.Property(v => v.FailureCategory)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired(false);

            builder.Property(v => v.ExpiresAtUtc)
                .IsRequired(false);

            builder.Property(v => v.DeletedAtUtc)
                .IsRequired(false);

            // Optimistic concurrency token — auto-incremented by SQL Server on every UPDATE
            builder.Property(v => v.RowVersion)
                .IsRowVersion()
                .IsRequired(false);
        }
    }
}
