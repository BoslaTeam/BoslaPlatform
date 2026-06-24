using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class MessageConfiguration
        : BaseEntityConfiguration<Message>
    {
        public override void Configure(EntityTypeBuilder<Message> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.MessageText)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.IsEdited)
                .HasDefaultValue(false);

            builder.HasIndex(x => x.SenderId);

            builder.HasIndex(x => x.ConversationId);

            builder.HasIndex(x => new
            {
                x.ConversationId,
                x.CreatedAtUtc
            });

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
