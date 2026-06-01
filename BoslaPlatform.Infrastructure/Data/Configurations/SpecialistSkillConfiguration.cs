using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class SpecialistSkillConfiguration : IEntityTypeConfiguration<SpecialistSkill>
    {
        public void Configure(EntityTypeBuilder<SpecialistSkill> builder)
        {
            builder.HasKey(ss => new { ss.SpecialistId, ss.SkillId });
            builder.HasOne(ss => ss.Specialist).WithMany(s => s.SpecialistSkills)
                .HasForeignKey(ss => ss.SpecialistId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(ss => ss.Skill).WithMany(s => s.SpecialistSkills)
                .HasForeignKey(ss => ss.SkillId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
