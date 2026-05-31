using BoslaPlatform.Domain.Models.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class EducationConfiguration : BaseEntityConfiguration<Education>
    {
        public override void Configure(EntityTypeBuilder<Education> builder)
        {
            builder.Property(e => e.InstitutionName).HasMaxLength(300).IsRequired();
            builder.Property(e => e.FieldOfStudy).HasMaxLength(200).IsRequired();
            builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
