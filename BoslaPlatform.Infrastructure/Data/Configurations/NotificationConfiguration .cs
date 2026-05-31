using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : BaseEntityConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
            builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(n => n.IsRead).HasDefaultValue(false);
            builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}
