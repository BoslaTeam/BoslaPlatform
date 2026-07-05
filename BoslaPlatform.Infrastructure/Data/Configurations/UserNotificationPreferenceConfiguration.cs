using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class UserNotificationPreferenceConfiguration : BaseEntityConfiguration<UserNotificationPreference>
    {
        public override void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(p => p.Enabled).HasDefaultValue(true);
            builder.HasOne(p => p.User)
                .WithMany(u => u.NotificationPreferences)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(p => new { p.UserId, p.Type }).IsUnique();
        }
    }
}
