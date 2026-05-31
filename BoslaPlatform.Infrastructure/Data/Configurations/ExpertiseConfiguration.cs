using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class ExpertiseConfiguration : BaseEntityConfiguration<Expertise>
    {
        public override void Configure(EntityTypeBuilder<Expertise> builder)
        {
            base.Configure(builder);
            builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(e => e.Name).IsUnique();
        }
    }
}
