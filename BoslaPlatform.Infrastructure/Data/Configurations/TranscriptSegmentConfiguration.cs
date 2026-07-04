using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class TranscriptSegmentConfiguration : BaseEntityConfiguration<TranscriptSegment>
    {
        public override void Configure(EntityTypeBuilder<TranscriptSegment> builder)
        {
            base.Configure(builder);

            builder.Property(ts => ts.TranscriptText)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(ts => ts.Language)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(ts => ts.SpeakerId)
                .HasMaxLength(100);

            builder.Property(ts => ts.SpeakerLabel)
                .HasMaxLength(100);

            builder.Property(ts => ts.SequenceNumber)
                .IsRequired();

            builder.Property(ts => ts.TimestampUtc)
                .IsRequired();

            builder.Property(ts => ts.Offset)
                .IsRequired();

            builder.HasOne(ts => ts.VideoSession)
                .WithMany(v => v.TranscriptSegments)
                .HasForeignKey(ts => ts.VideoSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ts => new { ts.VideoSessionId, ts.SequenceNumber })
                .IsUnique();

            builder.HasIndex(ts => new { ts.VideoSessionId, ts.TimestampUtc });
        }
    }
}
