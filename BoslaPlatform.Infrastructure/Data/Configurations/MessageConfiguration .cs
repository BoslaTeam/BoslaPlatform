using BoslaPlatform.Domain.Models.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class MessageConfiguration : BaseEntityConfiguration<Message>
    {
        public override void Configure(EntityTypeBuilder<Message> builder)
        {
            base.Configure(builder);
            builder.Property(m => m.MessageText).IsRequired();

            builder.HasOne(m => m.Conversation).WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
