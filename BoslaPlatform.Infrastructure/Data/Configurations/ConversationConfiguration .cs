using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
        public class ConversationConfiguration
        : BaseEntityConfiguration<Conversation>
        {
            public override void Configure(EntityTypeBuilder<Conversation> builder)
            {
                base.Configure(builder);

                builder.Property(x => x.AppointmentId)
                    .IsRequired();

                builder.HasIndex(x => x.AppointmentId);

                builder.Navigation(x => x.Participants)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);

                builder.Navigation(x => x.Messages)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            }
        }
}
