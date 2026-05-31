using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class IndustryConfiguration : BaseEntityConfiguration<Industry>
    {
        public override void Configure(EntityTypeBuilder<Industry> builder)
        {
            base.Configure(builder);
            builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(i => i.Name).IsUnique();
        }
    }
}
