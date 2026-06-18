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

                builder.HasOne(x => x.Appointment)
                    .WithMany()
                    .HasForeignKey(x => x.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.Navigation(x => x.Participants)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);

                builder.Navigation(x => x.Messages)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            }
        }
}
