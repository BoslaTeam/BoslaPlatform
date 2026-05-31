using BoslaPlatform.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoslaPlatform.Infrastructure.Data.Configurations
{
    public class SearchInteractionConfiguration : BaseEntityConfiguration<SearchInteraction>
    {
        public override void Configure(EntityTypeBuilder<SearchInteraction> builder)
        {
            builder.Property(si => si.RawQuery).HasMaxLength(1000).IsRequired();

            builder.HasOne(si => si.ClickedSpecialist).WithMany().HasForeignKey(si => si.ClickedSpecialistId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(si => si.User).WithMany().HasForeignKey(si => si.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
