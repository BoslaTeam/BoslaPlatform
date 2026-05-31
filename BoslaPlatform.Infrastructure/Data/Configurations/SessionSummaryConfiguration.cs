using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SessionSummaryConfiguration : BaseEntityConfiguration<SessionSummary>
    {
        public override void Configure(EntityTypeBuilder<SessionSummary> builder)
        {
            builder.Property(ss => ss.LlmProvider).HasMaxLength(50).IsRequired();
            builder.Property(ss => ss.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(ss => ss.Appointment).WithOne(a => a.SessionSummary)
                .HasForeignKey<SessionSummary>(ss => ss.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(ss => ss.Transcript).WithOne(t => t.Summary)
                .HasForeignKey<SessionSummary>(ss => ss.TranscriptId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
