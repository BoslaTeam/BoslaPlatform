using BoslaPlatform.Domain.Models.Junctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{

    public class UserIndustryConfiguration : IEntityTypeConfiguration<UserIndustry>
    {
        public void Configure(EntityTypeBuilder<UserIndustry> builder)
        {
            builder.HasKey(ui => new { ui.UserId, ui.IndustryId });
            builder.HasOne(ui => ui.Industry).WithMany(i => i.UserIndustries)
                .HasForeignKey(ui => ui.IndustryId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(ui => ui.User).WithMany().HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
