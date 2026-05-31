using BoslaPlatform.Domain.Models.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class ConversationConfiguration : BaseEntityConfiguration<Conversation>
    {
        public override void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.Title).HasMaxLength(200);
        }
    }
}
