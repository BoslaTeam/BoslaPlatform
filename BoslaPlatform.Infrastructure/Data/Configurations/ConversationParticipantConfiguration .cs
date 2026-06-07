using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ConversationParticipantConfiguration
        : BaseEntityConfiguration<ConversationParticipant>
    {
        public override void Configure(
            EntityTypeBuilder<ConversationParticipant> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new
            {
                x.ConversationId,
                x.UserId
            }).IsUnique();

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.ConversationParticipants)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
