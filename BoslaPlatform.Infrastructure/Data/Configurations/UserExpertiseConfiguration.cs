using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class UserExpertiseConfiguration : IEntityTypeConfiguration<UserExpertise>
    {
        public void Configure(EntityTypeBuilder<UserExpertise> builder)
        {
            builder.HasKey(ue => new { ue.UserId, ue.ExpertiseId });
            builder.HasOne(ue => ue.Expertise).WithMany(e => e.UserExpertise)
                .HasForeignKey(ue => ue.ExpertiseId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(ue => ue.User).WithMany().HasForeignKey(ue => ue.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
