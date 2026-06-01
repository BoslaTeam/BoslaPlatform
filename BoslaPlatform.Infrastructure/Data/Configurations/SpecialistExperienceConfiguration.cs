using BoslaPlatform.Domain.Models.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SpecialistExperienceConfiguration : BaseEntityConfiguration<SpecialistExperience>
    {
        public override void Configure(EntityTypeBuilder<SpecialistExperience> builder)
        {
            base.Configure(builder);
            builder.Property(e => e.JobTitle).HasMaxLength(200).IsRequired();
            builder.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(2000);

            builder.HasOne(e => e.Specialist).WithMany(s => s.Experiences)
                .HasForeignKey(e => e.SpecialistId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
