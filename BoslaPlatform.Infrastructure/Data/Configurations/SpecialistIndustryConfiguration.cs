using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SpecialistIndustryConfiguration : IEntityTypeConfiguration<SpecialistIndustry>
    {
        public void Configure(EntityTypeBuilder<SpecialistIndustry> builder)
        {
            builder.HasKey(si => new { si.SpecialistId, si.IndustryId });
            builder.HasOne(si => si.Specialist).WithMany(s => s.SpecialistIndustries)
                .HasForeignKey(si => si.SpecialistId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(si => si.Industry).WithMany(i => i.SpecialistIndustries)
                .HasForeignKey(si => si.IndustryId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
