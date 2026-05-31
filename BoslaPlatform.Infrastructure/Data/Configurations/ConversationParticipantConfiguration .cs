using BoslaPlatform.Domain.Models.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ConversationParticipantConfiguration : BaseEntityConfiguration<ConversationParticipant>
    {
        public override void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            base.Configure(builder);
            builder.Property(cp => cp.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(cp => cp.Conversation).WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.User)
                .WithMany(u => u.ConversationParticipants)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();
        }
    }
}
