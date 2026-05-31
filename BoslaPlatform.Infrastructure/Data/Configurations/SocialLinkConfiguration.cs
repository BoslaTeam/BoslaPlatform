using BoslaPlatform.Domain.Models.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SocialLinkConfiguration : BaseEntityConfiguration<SocialLink>
    {
        public override void Configure(EntityTypeBuilder<SocialLink> builder)
        {
            base.Configure(builder);
            builder.Property(s => s.Title).HasMaxLength(100).IsRequired();
            builder.Property(s => s.Url).HasMaxLength(500).IsRequired();

            builder.HasOne(s => s.User)
                .WithMany(u => u.SocialLinks)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
