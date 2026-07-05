using BoslaPlatform.Domain.Entities.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class PortfolioItemImageConfiguration : BaseEntityConfiguration<PortfolioItemImage>
    {
        public override void Configure(EntityTypeBuilder<PortfolioItemImage> builder)
        {
            base.Configure(builder);
            builder.Property(i => i.ImageUrl).HasMaxLength(1000).IsRequired();
            builder.Property(i => i.SortOrder).HasDefaultValue(0);
        }
    }
}
