using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SessionTranscriptConfiguration : BaseEntityConfiguration<SessionTranscript>
    {
        public override void Configure(EntityTypeBuilder<SessionTranscript> builder)
        {
            builder.Property(st => st.Language).HasMaxLength(10).IsRequired();

            builder.HasOne(st => st.VideoSession).WithOne()
                .HasForeignKey<SessionTranscript>(st => st.VideoSessionId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
