using BoslaPlatform.Domain.Models.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class VideoSessionParticipantConfiguration : BaseEntityConfiguration<VideoSessionParticipant>
    {
        public override void Configure(EntityTypeBuilder<VideoSessionParticipant> builder)
        {
            builder.Property(vp => vp.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(vp => vp.VideoSession).WithMany(v => v.Participants)
                .HasForeignKey(vp => vp.VideoSessionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(vp => vp.User).WithMany().HasForeignKey(vp => vp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(vp => new { vp.VideoSessionId, vp.UserId }).IsUnique();
            builder.HasIndex(vp => new { vp.AgoraUid, vp.VideoSessionId });
        }
    }
}
