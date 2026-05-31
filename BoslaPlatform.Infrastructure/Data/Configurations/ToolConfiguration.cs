using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class ToolConfiguration : BaseEntityConfiguration<Tool>
    {
        public override void Configure(EntityTypeBuilder<Tool> builder)
        {
            builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(t => t.Name).IsUnique();
        }
    }
}
