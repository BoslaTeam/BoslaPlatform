using BoslaPlatform.Domain.Models.Lookup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SkillConfiguration : BaseEntityConfiguration<Skill>
    {
        public override void Configure(EntityTypeBuilder<Skill> builder)
        {
            base.Configure(builder);
            builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
