using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SpecialistToolConfiguration : IEntityTypeConfiguration<SpecialistTool>
    {
        public void Configure(EntityTypeBuilder<SpecialistTool> builder)
        {
            builder.HasKey(st => new { st.SpecialistId, st.ToolId });
            builder.HasOne(st => st.Specialist).WithMany(s => s.SpecialistTools)
                .HasForeignKey(st => st.SpecialistId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(st => st.Tool).WithMany(t => t.SpecialistTools)
                .HasForeignKey(st => st.ToolId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
