using BoslaPlatform.Domain.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);
            builder.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
            builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
            builder.HasIndex(rt => rt.Token).IsUnique();

            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
