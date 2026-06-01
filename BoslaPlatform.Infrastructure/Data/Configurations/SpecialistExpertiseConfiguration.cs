using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SpecialistExpertiseConfiguration : IEntityTypeConfiguration<SpecialistExpertise>
    {
        public void Configure(EntityTypeBuilder<SpecialistExpertise> builder)
        {
            builder.HasKey(se => new { se.SpecialistId, se.ExpertiseId });
            builder.HasOne(se => se.Specialist).WithMany(s => s.SpecialistExpertise)
                .HasForeignKey(se => se.SpecialistId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(se => se.Expertise).WithMany(e => e.SpecialistExpertise)
                .HasForeignKey(se => se.ExpertiseId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
